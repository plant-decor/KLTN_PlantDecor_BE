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
        private IPlantGuideRepository? _plantGuideRepository;
        private IMaterialRepository? _materialRepository;
        private IRoleRepository? _roleRepository;
        private ICommonPlantRepository? _commonPlantRepository;
        private IPlantComboRepository? _plantComboRepository;
        private INurseryRepository? _nurseryRepository;
        private INurseryMaterialRepository? _nurseryMaterialRepository;
        private IPlantInstanceRepository? _plantInstanceRepository;
        private INurseryPlantComboRepository? _nurseryPlantComboRepository;
        private ICartRepository? _cartRepository;
        private IWishlistRepository? _wishlistRepository;
        private IPaymentRepository? _paymentRepository;
        private ITransactionRepository? _transactionRepository;
        private IOrderRepository? _orderRepository;
        private INurseryOrderRepository? _nurseryOrderRepository;
        private IInvoiceRepository? _invoiceRepository;
        private IUserBehaviorLogRepository? _userBehaviorLogRepository;
        private IUserPlantRepository? _userPlantRepository;
        private IAIChatSessionRepository? _aiChatSessionRepository;
        private IAIChatMessageRepository? _aiChatMessageRepository;
        private IPolicyContentRepository? _policyContentRepository;
        private IChatSessionRepository? _chatSessionRepository;
        private IChatMessageRepository? _chatMessageRepository;
        private IChatParticipantRepository? _chatParticipantRepository;
        private IEmbeddingRepository? _embeddingRepository;
        private IRoomImageRepository? _roomImageRepository;
        private IRoomDesignPreferencesRepository? _roomDesignPreferencesRepository;
        private ILayoutDesignRepository? _layoutDesignRepository;
        private ILayoutDesignAiResponseImageRepository? _layoutDesignAiResponseImageRepository;
        private ILayoutDesignPlantRepository? _layoutDesignPlantRepository;
        private IDesignTemplateRepository? _designTemplateRepository;
        private IDesignTemplateTierRepository? _designTemplateTierRepository;
        private IDesignTemplateTierItemRepository? _designTemplateTierItemRepository;
        private IDesignTemplateSpecializationRepository? _designTemplateSpecializationRepository;
        private INurseryDesignTemplateRepository? _nurseryDesignTemplateRepository;
        private IDesignRegistrationRepository? _designRegistrationRepository;
        private IDesignTaskRepository? _designTaskRepository;
        private ITaskMaterialUsageRepository? _taskMaterialUsageRepository;
        private IAiLayoutResponseModerationRepository? _aiLayoutResponseModerationRepository;
        private IRoomUploadModerationRepository? _roomUploadModerationRepository;
        private IServiceRegistrationRepository? _serviceRegistrationRepository;
        private IServiceProgressRepository? _serviceProgressRepository;
        private INurseryCareServiceRepository? _nurseryCareServiceRepository;
        private ICareServicePackageRepository? _careServicePackageRepository;
        private ISpecializationRepository? _specializationRepository;
        private IServiceRatingRepository? _serviceRatingRepository;
        private IShiftRepository? _shiftRepository;
        private IDepositPolicyRepository? _depositPolicyRepository;
        private IReturnTicketRepository? _returnTicketRepository;
        private IReturnTicketAssignmentRepository? _returnTicketAssignmentRepository;

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

        public IPlantGuideRepository PlantGuideRepository
        {
            get { return _plantGuideRepository ??= new PlantGuideRepository(_context); }
        }

        public IMaterialRepository MaterialRepository
        {
            get { return _materialRepository ??= new MaterialRepository(_context); }
        }

        public IRoleRepository RoleRepository
        {
            get { return _roleRepository ??= new RoleRepository(_context); }
        }
        public ICommonPlantRepository CommonPlantRepository
        {
            get { return _commonPlantRepository ??= new CommonPlantRepository(_context); }
        }

        public IPlantComboRepository PlantComboRepository
        {
            get { return _plantComboRepository ??= new PlantComboRepository(_context); }
        }

        public INurseryRepository NurseryRepository
        {
            get { return _nurseryRepository ??= new NurseryRepository(_context); }
        }

        public INurseryMaterialRepository NurseryMaterialRepository
        {
            get { return _nurseryMaterialRepository ??= new NurseryMaterialRepository(_context); }
        }

        public IPlantInstanceRepository PlantInstanceRepository
        {
            get { return _plantInstanceRepository ??= new PlantInstanceRepository(_context); }
        }

        public INurseryPlantComboRepository NurseryPlantComboRepository
        {
            get { return _nurseryPlantComboRepository ??= new NurseryPlantComboRepository(_context); }
        }

        public ICartRepository CartRepository
        {
            get { return _cartRepository ??= new CartRepository(_context); }
        }

        public IWishlistRepository WishlistRepository
        {
            get { return _wishlistRepository ??= new WishlistRepository(_context); }
        }

        public IPaymentRepository PaymentRepository
        {
            get { return _paymentRepository ??= new PaymentRepository(_context); }
        }

        public ITransactionRepository TransactionRepository
        {
            get { return _transactionRepository ??= new TransactionRepository(_context); }
        }

        public IOrderRepository OrderRepository
        {
            get { return _orderRepository ??= new OrderRepository(_context); }
        }

        public INurseryOrderRepository NurseryOrderRepository
        {
            get { return _nurseryOrderRepository ??= new NurseryOrderRepository(_context); }
        }

        public IInvoiceRepository InvoiceRepository
        {
            get { return _invoiceRepository ??= new InvoiceRepository(_context); }
        }

        public IUserBehaviorLogRepository UserBehaviorLogRepository
        {
            get { return _userBehaviorLogRepository ??= new UserBehaviorLogRepository(_context); }
        }

        public IUserPlantRepository UserPlantRepository
        {
            get { return _userPlantRepository ??= new UserPlantRepository(_context); }
        }

        public IAIChatSessionRepository AIChatSessionRepository
        {
            get { return _aiChatSessionRepository ??= new AIChatSessionRepository(_context); }
        }

        public IAIChatMessageRepository AIChatMessageRepository
        {
            get { return _aiChatMessageRepository ??= new AIChatMessageRepository(_context); }
        }

        public IPolicyContentRepository PolicyContentRepository
        {
            get { return _policyContentRepository ??= new PolicyContentRepository(_context); }
        }

        public IChatSessionRepository ChatSessionRepository
        {
            get { return _chatSessionRepository ??= new ChatSessionRepository(_context); }
        }

        public IChatMessageRepository ChatMessageRepository
        {
            get { return _chatMessageRepository ??= new ChatMessageRepository(_context); }
        }

        public IChatParticipantRepository ChatParticipantRepository
        {
            get { return _chatParticipantRepository ??= new ChatParticipantRepository(_context); }
        }

        public IEmbeddingRepository EmbeddingRepository
        {
            get { return _embeddingRepository ??= new EmbeddingRepository(_context); }
        }

        public IRoomImageRepository RoomImageRepository
        {
            get { return _roomImageRepository ??= new RoomImageRepository(_context); }
        }

        public IRoomDesignPreferencesRepository RoomDesignPreferencesRepository
        {
            get { return _roomDesignPreferencesRepository ??= new RoomDesignPreferencesRepository(_context); }
        }

        public ILayoutDesignRepository LayoutDesignRepository
        {
            get { return _layoutDesignRepository ??= new LayoutDesignRepository(_context); }
        }

        public ILayoutDesignAiResponseImageRepository LayoutDesignAiResponseImageRepository
        {
            get { return _layoutDesignAiResponseImageRepository ??= new LayoutDesignAiResponseImageRepository(_context); }
        }

        public ILayoutDesignPlantRepository LayoutDesignPlantRepository
        {
            get { return _layoutDesignPlantRepository ??= new LayoutDesignPlantRepository(_context); }
        }

        public IDesignTemplateRepository DesignTemplateRepository
        {
            get { return _designTemplateRepository ??= new DesignTemplateRepository(_context); }
        }

        public IDesignTemplateTierRepository DesignTemplateTierRepository
        {
            get { return _designTemplateTierRepository ??= new DesignTemplateTierRepository(_context); }
        }

        public IDesignTemplateTierItemRepository DesignTemplateTierItemRepository
        {
            get { return _designTemplateTierItemRepository ??= new DesignTemplateTierItemRepository(_context); }
        }

        public IDesignTemplateSpecializationRepository DesignTemplateSpecializationRepository
        {
            get { return _designTemplateSpecializationRepository ??= new DesignTemplateSpecializationRepository(_context); }
        }

        public INurseryDesignTemplateRepository NurseryDesignTemplateRepository
        {
            get { return _nurseryDesignTemplateRepository ??= new NurseryDesignTemplateRepository(_context); }
        }

        public IDesignRegistrationRepository DesignRegistrationRepository
        {
            get { return _designRegistrationRepository ??= new DesignRegistrationRepository(_context); }
        }

        public IDesignTaskRepository DesignTaskRepository
        {
            get { return _designTaskRepository ??= new DesignTaskRepository(_context); }
        }

        public ITaskMaterialUsageRepository TaskMaterialUsageRepository
        {
            get { return _taskMaterialUsageRepository ??= new TaskMaterialUsageRepository(_context); }
        }

        public IAiLayoutResponseModerationRepository AiLayoutResponseModerationRepository
        {
            get { return _aiLayoutResponseModerationRepository ??= new AiLayoutResponseModerationRepository(_context); }
        }

        public IRoomUploadModerationRepository RoomUploadModerationRepository
        {
            get { return _roomUploadModerationRepository ??= new RoomUploadModerationRepository(_context); }
        }

        public IServiceRegistrationRepository ServiceRegistrationRepository
        {
            get { return _serviceRegistrationRepository ??= new ServiceRegistrationRepository(_context); }
        }

        public IServiceProgressRepository ServiceProgressRepository
        {
            get { return _serviceProgressRepository ??= new ServiceProgressRepository(_context); }
        }

        public INurseryCareServiceRepository NurseryCareServiceRepository
        {
            get { return _nurseryCareServiceRepository ??= new NurseryCareServiceRepository(_context); }
        }

        public ICareServicePackageRepository CareServicePackageRepository
        {
            get { return _careServicePackageRepository ??= new CareServicePackageRepository(_context); }
        }

        public ISpecializationRepository SpecializationRepository
        {
            get { return _specializationRepository ??= new SpecializationRepository(_context); }
        }

        public IServiceRatingRepository ServiceRatingRepository
        {
            get { return _serviceRatingRepository ??= new ServiceRatingRepository(_context); }
        }

        public IShiftRepository ShiftRepository
        {
            get { return _shiftRepository ??= new ShiftRepository(_context); }
        }

        public IDepositPolicyRepository DepositPolicyRepository
        {
            get { return _depositPolicyRepository ??= new DepositPolicyRepository(_context); }
        }

        public IReturnTicketRepository ReturnTicketRepository
        {
            get { return _returnTicketRepository ??= new ReturnTicketRepository(_context); }
        }

        public IReturnTicketAssignmentRepository ReturnTicketAssignmentRepository
        {
            get { return _returnTicketAssignmentRepository ??= new ReturnTicketAssignmentRepository(_context); }
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
