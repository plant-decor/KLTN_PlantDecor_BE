using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class TokenCleanupService : ITokenCleanupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TokenCleanupService> _logger;

        public TokenCleanupService(IUnitOfWork unitOfWork, ILogger<TokenCleanupService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task CleanupRevokedTokensAsync()
        {
            var deletedCount = await _unitOfWork.UserRepository.DeleteRevokedRefreshTokensAsync();
            _logger.LogInformation("Token cleanup job: {Count} revoked refresh token(s) deleted", deletedCount);
        }
    }
}
