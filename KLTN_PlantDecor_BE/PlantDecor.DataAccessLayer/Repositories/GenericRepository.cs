using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected PlantDecorContext _context;

        public GenericRepository(PlantDecorContext context) => _context = context;
        public List<T> GetAll()
        {
            return _context.Set<T>().ToList();
        }

        public void CreateT(T entity)
        {
            _context.Add(entity);
            _context.SaveChanges();
        }

        public void UpdateT(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            _context.SaveChanges();
        }

        public bool Remove(T entity)
        {
            _context.Remove(entity);
            _context.SaveChanges();
            return true;
        }

        public T GetById(int id)
        {
            return _context.Set<T>().Find(id);
        }

        public T GetById(string code)
        {
            return _context.Set<T>().Find(code);
        }

        public T GetById(Guid code)
        {
            return _context.Set<T>().Find(code);
        }

        #region Asynchronous
        public virtual async Task<List<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }

        public virtual async Task<PaginatedResult<T>> GetAllAsyncWithPagination(Pagination pagination)
        {
            // Get total count
            var totalCount = await _context.Set<T>().CountAsync();

            // Get paginated items
            var items = await _context.Set<T>()
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<T>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public virtual async Task<int> CreateAsync(T entity)
        {

            _context.Add(entity);
            return await _context.SaveChangesAsync();

        }

        public virtual async Task<int> UpdateAsync(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            return await _context.SaveChangesAsync();
        }

        public async Task<int> UpdateRangeAsync(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                var tracker = _context.Attach(entity);
                tracker.State = EntityState.Modified;
            }

            return await _context.SaveChangesAsync();
        }

        public async Task<bool> RemoveAsync(T entity)
        {
            _context.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public virtual async Task<T> GetByIdAsync(int id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public async Task<T> GetByIdAsync(string code)
        {
            return await _context.Set<T>().FindAsync(code);
        }

        public async Task<T> GetByIdAsync(Guid code)
        {
            return await _context.Set<T>().FindAsync(code);
        }
        #endregion

        #region Separating asigned entities and save operators        

        public void PrepareCreate(T entity)
        {
            _context.Add(entity);
        }

        public void PrepareUpdate(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        public void PrepareRemove(T entity)
        {
            _context.Remove(entity);
        }

        public int Save()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion Separating asign entity and save operators
    }
}
