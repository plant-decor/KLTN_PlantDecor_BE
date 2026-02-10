using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICacheService
    {
        // Lấy dữ liệu từ Cache
        Task<T> GetDataAsync<T>(string key);

        // Lưu dữ liệu vào Cache
        Task SetDataAsync<T>(string key, T value, DateTimeOffset expirationTime);

        // Xóa dữ liệu
        Task<object> RemoveDataAsync(string key);
    }
}
