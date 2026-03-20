using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using PlantDecor.BusinessLogicLayer.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly string _instanceName;

        public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer connectionMultiplexer)
        {
            _cache = cache;
            _connectionMultiplexer = connectionMultiplexer;
            _instanceName = "PlantDecor_";
        }

        public async Task<T> GetDataAsync<T>(string key)
        {
            var value = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(value))
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            return default;
        }

        public async Task<object> RemoveDataAsync(string key)
        {
            await _cache.RemoveAsync(key);
            return false;
        }

        public async Task SetDataAsync<T>(string key, T value, DateTimeOffset expirationTime)
        {
            var expiryTime = expirationTime.DateTime.Subtract(DateTime.Now);
            var serializedData = JsonConvert.SerializeObject(value);

            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiryTime,
                SlidingExpiration = TimeSpan.FromMinutes(20) // Tùy chỉnh
            };

            await _cache.SetStringAsync(key, serializedData, options);
        }

        public async Task RemoveByPrefixAsync(string prefixKey)
        {
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
            var pattern = $"{_instanceName}{prefixKey}*";
            var db = _connectionMultiplexer.GetDatabase();

            await foreach (var key in server.KeysAsync(pattern: pattern))
            {
                await db.KeyDeleteAsync(key);
            }
        }
    }
}
