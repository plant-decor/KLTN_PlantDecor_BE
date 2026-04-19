using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Libraries;
using PlantDecor.DataAccessLayer.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmailBackgroundJobService : IEmailBackgroundJobService
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailBackgroundJobService> _logger;

        public EmailBackgroundJobService(
            IAuthenticationService authenticationService,
            IUnitOfWork unitOfWork,
            IEmailService emailService,
            ILogger<EmailBackgroundJobService> logger)
        {
            _authenticationService = authenticationService;
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string email)
        {
            var request = new ResendVerifyRequest { Email = email };
            await _authenticationService.VerifyEmailAsync(request, CancellationToken.None);
        }

        public async Task SendOrderSuccessEmailAsync(int orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Cannot send order success email because OrderId={OrderId} was not found", orderId);
                    return;
                }

                var user = await _unitOfWork.UserRepository.GetByIdAsync(order.UserId);
                var toEmail = user?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    toEmail = order.Invoices
                        .Select(i => i.CustomerEmail)
                        .FirstOrDefault(email => !string.IsNullOrWhiteSpace(email));
                }

                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("Cannot send order success email because no receiver email was found for OrderId={OrderId}", orderId);
                    return;
                }

                var userName = !string.IsNullOrWhiteSpace(user?.Username)
                    ? user!.Username!
                    : order.CustomerName ?? "Khach hang";

                var productNames = order.NurseryOrders
                    .SelectMany(no => no.NurseryOrderDetails)
                    .Select(detail => detail.ItemName
                        ?? detail.CommonPlant?.Plant?.Name
                        ?? detail.PlantInstance?.Plant?.Name
                        ?? detail.NurseryMaterial?.Material?.Name
                        ?? detail.NurseryPlantCombo?.PlantCombo?.ComboName)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct()
                    .ToList();

                var productDisplay = productNames.Count == 0
                    ? "San pham PlantDecor"
                    : string.Join(", ", productNames.Take(3));

                if (productNames.Count > 3)
                {
                    productDisplay = $"{productDisplay} va {productNames.Count - 3} san pham khac";
                }

                var amountDisplay = (order.TotalAmount ?? 0m).ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " VND";
                var orderDate = (order.CreatedAt ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm");

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"Thanh toan thanh cong - Don hang #{order.Id}",
                    Body = EmailOrderSuccessTemplate.OrderSuccessTemplate(
                        userName,
                        order.Id.ToString(),
                        amountDisplay,
                        orderDate,
                        productDisplay)
                }, CancellationToken.None);

                _logger.LogInformation("Sent order success email for OrderId={OrderId} to {Email}", order.Id, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order success email for OrderId={OrderId}", orderId);
                throw;
            }
        }
    }
}
