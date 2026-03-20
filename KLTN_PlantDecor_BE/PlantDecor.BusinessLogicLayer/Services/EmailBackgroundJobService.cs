using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmailBackgroundJobService : IEmailBackgroundJobService
    {
        private readonly IAuthenticationService _authenticationService;

        public EmailBackgroundJobService(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public async Task SendVerificationEmailAsync(string email)
        {
            var request = new ResendVerifyRequest { Email = email };
            await _authenticationService.VerifyEmailAsync(request, CancellationToken.None);
        }

        public Task SendOrderSuccessEmailAsync(int orderId)
        {
            throw new NotImplementedException();
        }
    }
}
