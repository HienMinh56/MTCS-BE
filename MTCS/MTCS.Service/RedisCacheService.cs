using System.Text.Json;
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
        Task InvalidateTractorCache();
    }

    public class RedisCacheService : IRedisCacheService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RedisCacheService> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly TimeSpan _defaultExpiry;

        public RedisCacheService(
            IConfiguration configuration,
            ILogger<RedisCacheService> logger,
            IConnectionMultiplexer redis)
        {
            _configuration = configuration;
            _logger = logger;
            _redis = redis;
            _db = redis.GetDatabase();

            // Get default expiry from configuration or use 30 minutes
            int minutes = configuration.GetValue<int>("Redis:DefaultExpiryMinutes");
            _defaultExpiry = TimeSpan.FromMinutes(minutes > 0 ? minutes : 30);
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedValue = await _db.StringGetAsync(key);

                if (cachedValue.IsNullOrEmpty)
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
                var expiryTime = expiry ?? _defaultExpiry;
                string serializedValue = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, serializedValue, expiryTime);
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
                await _db.KeyDeleteAsync(key);
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
                return await _db.KeyExistsAsync(key);
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
                // Get all endpoints and use the first one to get the server instance
                var endPoints = _redis.GetEndPoints();
                if (endPoints.Length == 0)
                {
                    _logger.LogWarning("No Redis endpoints available for RemoveByPrefixAsync");
                    return;
                }

                var server = _redis.GetServer(endPoints[0]);
                var keys = server.Keys(pattern: $"{prefix}*");

                // Delete all keys with the given prefix
                foreach (var key in keys)
                {
                    await _db.KeyDeleteAsync(key);
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

        public async Task InvalidateTractorCache()
        {
            await RemoveByPrefixAsync("tractor:");
        }
    }
}
