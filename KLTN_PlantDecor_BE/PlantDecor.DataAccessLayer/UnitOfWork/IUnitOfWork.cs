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
        IPlantGuideRepository PlantGuideRepository { get; }
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
        INurseryOrderRepository NurseryOrderRepository { get; }
        IInvoiceRepository InvoiceRepository { get; }
        IUserBehaviorLogRepository UserBehaviorLogRepository { get; }
        IUserPlantRepository UserPlantRepository { get; }
        ICareReminderRepository CareReminderRepository { get; }
        IAIChatSessionRepository AIChatSessionRepository { get; }
        IAIChatMessageRepository AIChatMessageRepository { get; }
        IPolicyContentRepository PolicyContentRepository { get; }
        IChatSessionRepository ChatSessionRepository { get; }
        IChatMessageRepository ChatMessageRepository { get; }
        IChatParticipantRepository ChatParticipantRepository { get; }
        IEmbeddingRepository EmbeddingRepository { get; }
        IRoomImageRepository RoomImageRepository { get; }
        IRoomDesignPreferencesRepository RoomDesignPreferencesRepository { get; }
        ILayoutDesignRepository LayoutDesignRepository { get; }
        ILayoutDesignAiResponseImageRepository LayoutDesignAiResponseImageRepository { get; }
        ILayoutDesignPlantRepository LayoutDesignPlantRepository { get; }
        IDesignTemplateRepository DesignTemplateRepository { get; }
        IDesignTemplateTierRepository DesignTemplateTierRepository { get; }
        IDesignTemplateTierItemRepository DesignTemplateTierItemRepository { get; }
        IDesignTemplateSpecializationRepository DesignTemplateSpecializationRepository { get; }
        INurseryDesignTemplateRepository NurseryDesignTemplateRepository { get; }
        IDesignRegistrationRepository DesignRegistrationRepository { get; }
        IDesignTaskRepository DesignTaskRepository { get; }
        ITaskMaterialUsageRepository TaskMaterialUsageRepository { get; }
        IAiLayoutResponseModerationRepository AiLayoutResponseModerationRepository { get; }
        IRoomUploadModerationRepository RoomUploadModerationRepository { get; }
        IServiceRegistrationRepository ServiceRegistrationRepository { get; }
        IServiceProgressRepository ServiceProgressRepository { get; }
        INurseryCareServiceRepository NurseryCareServiceRepository { get; }
        ICareServicePackageRepository CareServicePackageRepository { get; }
        ISpecializationRepository SpecializationRepository { get; }
        IServiceRatingRepository ServiceRatingRepository { get; }
        IShiftRepository ShiftRepository { get; }
        IDepositPolicyRepository DepositPolicyRepository { get; }
        IReturnTicketRepository ReturnTicketRepository { get; }
        IReturnTicketAssignmentRepository ReturnTicketAssignmentRepository { get; }

        // Transaction management
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();

        // Save changes
        Task<int> SaveAsync();
        //Không cần viết lại DisposeAsync() vì nó đã được kế thừa từ IAsyncDisposable.
    }
}
