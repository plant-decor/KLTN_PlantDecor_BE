using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using PlantDecor.BusinessLogicLayer.Interfaces;
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
        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
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
    }
}
