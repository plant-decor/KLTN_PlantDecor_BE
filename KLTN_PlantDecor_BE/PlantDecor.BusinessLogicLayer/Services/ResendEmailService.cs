using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Interfaces;
using Resend;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ResendEmailService : IEmailService
    {
        private readonly IResend _resend;
        private readonly IConfiguration _configuration;
        private readonly string _fromEmail;
        private readonly string _fromName;
        public ResendEmailService(IConfiguration configuration, IResend resend)
        {
            _configuration = configuration;
            _fromEmail = _configuration["Resend:FromEmail"];
            _fromName = _configuration["Resend:FromName"];
            _resend = resend;
        }
        public async Task SendEmailAsync(EmailRequest emailRequest, CancellationToken cancellationToken)
        {
            try
            {
                var message = new EmailMessage();
                message.From = $"{_fromName} <{_fromEmail}>";
                message.To.Add(emailRequest.To);
                message.Subject = emailRequest.Subject;
                message.HtmlBody = emailRequest.Body;

                var response = await _resend.EmailSendAsync(message, cancellationToken);

                if (!response.Success)
                {
                    throw new Exception($"Failed to send email via Resend: {response.Exception.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error sending email via Resend: {ex.Message}", ex);
            }
        }
    }
}
