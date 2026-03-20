using PlantDecor.BusinessLogicLayer.DTOs.Requests;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailRequest emailRequest, CancellationToken cancellationToken);
    }
}
