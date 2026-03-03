using Microsoft.EntityFrameworkCore.Storage;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Interfaces;
using PlantDecor.DataAccessLayer.Repositories;

namespace PlantDecor.DataAccessLayer.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PlantDecorContext _context;
        private IDbContextTransaction _transaction;


        // Repository instances
        private IUserRepository? _userRepository;
        private ICategoryRepository? _categoryRepository;
        private ITagRepository? _tagRepository;
        private IPlantRepository? _plantRepository;
        private IPlantInstanceRepository? _plantInstanceRepository;
        private IInventoryRepository? _inventoryRepository;
        private IRoleRepository? _roleRepository;
        private IPlantInventoryRepository? _plantInventoryRepository;
        private IPlantComboRepository? _plantComboRepository;

        public UnitOfWork(PlantDecorContext context)
        {
            _context = context;
        }

        //Lazy loading of repositories
        public IUserRepository UserRepository
        {
            get { return _userRepository ??= new UserRepository(_context); }
        }

        public ICategoryRepository CategoryRepository
        {
            get { return _categoryRepository ??= new CategoryRepository(_context); }
        }

        public ITagRepository TagRepository
        {
            get { return _tagRepository ??= new TagRepository(_context); }
        }

        public IPlantRepository PlantRepository
        {
            get { return _plantRepository ??= new PlantRepository(_context); }
        }

        public IPlantInstanceRepository PlantInstanceRepository
        {
            get { return _plantInstanceRepository ??= new PlantInstanceRepository(_context); }
        }

        public IInventoryRepository InventoryRepository
        {
            get { return _inventoryRepository ??= new InventoryRepository(_context); }
        }

        public IRoleRepository RoleRepository
        {
            get { return _roleRepository ??= new RoleRepository(_context); }
        }
        public IPlantInventoryRepository PlantInventoryRepository
        {
            get { return _plantInventoryRepository ??= new PlantInventoryRepository(_context); }
        }

        public IPlantComboRepository PlantComboRepository
        {
            get { return _plantComboRepository ??= new PlantComboRepository(_context); }
        }

        // Transaction Management
        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        // Async Dispose
        public async ValueTask DisposeAsync()
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }

            await _context.DisposeAsync();
        }
    }
}
