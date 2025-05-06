using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MTCS.Service.Cache
{
    public interface IRedisCacheService
    {
        Task<T> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
        Task RemoveAsync(string key);
        Task<bool> ExistsAsync(string key);
        Task RemoveByPrefixAsync(string prefix);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> dataFactory, TimeSpan? expiry = null) where T : class;
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly TimeSpan _defaultExpiry;

        public RedisCacheService(
            IDistributedCache cache,
            IConfiguration configuration,
            ILogger<RedisCacheService> logger,
            IConnectionMultiplexer redis)
        {
            _cache = cache;
            _configuration = configuration;
            _logger = logger;
            _redis = redis;

            // Get default expiry from configuration or use 30 minutes
            int minutes = configuration.GetValue<int>("Redis:DefaultExpiryMinutes");
            _defaultExpiry = TimeSpan.FromMinutes(minutes > 0 ? minutes : 30);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            try
            {
                string cachedValue = await _cache.GetStringAsync(key);

                if (string.IsNullOrEmpty(cachedValue))
                {
                    return null;
                }

                return JsonSerializer.Deserialize<T>(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting value from Redis cache for key {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry ?? _defaultExpiry
                };

                string serializedValue = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serializedValue, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting value in Redis cache for key {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing value from Redis cache for key {Key}", key);
            }
        }

        public async Task<bool> ExistsAsync(string key)
        {
            try
            {
                return await _redis.GetDatabase().KeyExistsAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if key exists in Redis cache: {Key}", key);
                return false;
            }
        }

        public async Task RemoveByPrefixAsync(string prefix)
        {
            try
            {
                var server = _redis.GetServer(_redis.GetEndPoints()[0]);
                var keys = server.Keys(pattern: $"{prefix}*");

                var db = _redis.GetDatabase();
                foreach (var key in keys)
                {
                    await db.KeyDeleteAsync(key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing keys by prefix from Redis cache: {Prefix}", prefix);
            }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> dataFactory, TimeSpan? expiry = null) where T : class
        {
            try
            {
                // Try to get from cache first
                var cachedValue = await GetAsync<T>(key);
                if (cachedValue != null)
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return cachedValue;
                }

                _logger.LogDebug("Cache miss for key: {Key}", key);

                var data = await dataFactory();

                if (data != null)
                {
                    await SetAsync(key, data, expiry);
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrSetAsync for key {Key}", key);

                return await dataFactory();
            }
        }

    }
}
