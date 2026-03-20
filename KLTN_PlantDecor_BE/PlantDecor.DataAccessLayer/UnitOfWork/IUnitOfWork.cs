using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.UnitOfWork
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        // Repository access
        IUserRepository UserRepository { get; }
        IRoleRepository RoleRepository { get; }
        ICategoryRepository CategoryRepository { get; }
        ITagRepository TagRepository { get; }
        IPlantRepository PlantRepository { get; }
        IMaterialRepository MaterialRepository { get; }
        ICommonPlantRepository CommonPlantRepository { get; }
        IPlantComboRepository PlantComboRepository { get; }
        INurseryRepository NurseryRepository { get; }
        INurseryMaterialRepository NurseryMaterialRepository { get; }
        IPlantInstanceRepository PlantInstanceRepository { get; }
        INurseryPlantComboRepository NurseryPlantComboRepository { get; }
        ICartRepository CartRepository { get; }
        IWishlistRepository WishlistRepository { get; }
        IPaymentRepository PaymentRepository { get; }
        ITransactionRepository TransactionRepository { get; }
        IOrderRepository OrderRepository { get; }
        IInvoiceRepository InvoiceRepository { get; }

        // Transaction management
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Save changes
        Task<int> SaveAsync();
        //Không cần viết lại DisposeAsync() vì nó đã được kế thừa từ IAsyncDisposable.
    }
}
