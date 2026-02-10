using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        List<T> GetAll();
        void CreateT(T entity);
        void UpdateT(T entity);
        bool Remove(T entity);

        T GetById(int id);
        T GetById(string code);
        T GetById(Guid code);

        Task<List<T>> GetAllAsync();
        Task<int> CreateAsync(T entity);
        Task<int> UpdateAsync(T entity);
        Task<int> UpdateRangeAsync(IEnumerable<T> entities);
        Task<bool> RemoveAsync(T entity);

        Task<T> GetByIdAsync(int id);
        Task<T> GetByIdAsync(string code);
        Task<T> GetByIdAsync(Guid code);

        void PrepareCreate(T entity);
        void PrepareUpdate(T entity);
        void PrepareRemove(T entity);

        int Save();
        Task<int> SaveAsync();
    }
}
