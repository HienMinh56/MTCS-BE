using System.Text;

namespace MTCS.Data.Helpers
{
    public class CacheKeyBuilder
    {
        public static string BuildCacheKey(string entityType, string operation, object parameters)
        {
            var keyBuilder = new StringBuilder($"{entityType}:{operation}");

            if (parameters != null)
            {
                var properties = parameters.GetType().GetProperties();
                foreach (var prop in properties)
                {
                    var value = prop.GetValue(parameters)?.ToString() ?? "none";
                    keyBuilder.Append($":{prop.Name}:{value}");
                }
            }

            return keyBuilder.ToString();
        }
    }
}
