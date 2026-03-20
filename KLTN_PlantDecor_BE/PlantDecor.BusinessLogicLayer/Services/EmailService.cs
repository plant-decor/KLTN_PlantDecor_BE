using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Net;
using System.Net.Mail;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(EmailRequest emailRequest, CancellationToken cancellationToken)
        {
            var from = _configuration["EmailSettings:From"];
            var smtpServer = _configuration["EmailSettings:Server"];
            var port = int.Parse(_configuration["EmailSettings:Port"]);
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            var message = new MailMessage(from, emailRequest.To, emailRequest.Subject, emailRequest.Body);
            message.IsBodyHtml = true;
            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true
            };

            if (emailRequest.AttachmentFilePaths?.Length > 0)
            {
                foreach (var filePath in emailRequest.AttachmentFilePaths)
                {
                    var attachment = new Attachment(filePath);
                    message.Attachments.Add(attachment);
                }
            }

            await client.SendMailAsync(message, cancellationToken);


        }
    }
}
