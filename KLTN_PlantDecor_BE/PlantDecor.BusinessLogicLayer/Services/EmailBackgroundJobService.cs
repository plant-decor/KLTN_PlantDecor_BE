using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Libraries;
using PlantDecor.DataAccessLayer.UnitOfWork;
using PlantDecor.DataAccessLayer.Enums;
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

        public async Task SendOtpEmailVerificationAsync(string email)
        {
            var request = new SendOtpEmailVerificationRequest { Email = email };
            var result = await _authenticationService.SendOtpEmailVerificationAsync(request, CancellationToken.None);

            if (!result.Success)
            {
                _logger.LogWarning("Failed to send OTP verification email for {Email}: {Message}", email, result.Message);
            }
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

        // ──────────────────────────────────────────────────────────────────────────
        // #1  Khách tạo đơn thành công
        // ──────────────────────────────────────────────────────────────────────────
        public async Task SendServiceRegistrationCreatedEmailAsync(int registrationId)
        {
            try
            {
                var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
                if (registration == null)
                {
                    _logger.LogWarning("SendServiceRegistrationCreatedEmail: Registration {Id} not found", registrationId);
                    return;
                }

                var toEmail = registration.User?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendServiceRegistrationCreatedEmail: No email for registration {Id}", registrationId);
                    return;
                }

                var userName = registration.User?.Username ?? "Customer";
                var packageName = registration.NurseryCareService?.CareServicePackage?.Name ?? "Care Service";
                var serviceDate = registration.ServiceDate?.ToString("dd/MM/yyyy") ?? "Not specified";
                var nurseryName = registration.NurseryCareService?.Nursery?.Name ?? "PlantDecor Nursery";
                var status = registration.Status switch
                {
                    (int)ServiceRegistrationStatusEnum.PendingApproval => "Pending Approval",
                    (int)ServiceRegistrationStatusEnum.WaitingForNursery => "Looking for suitable nursery",
                    _ => "Processing"
                };

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"[PlantDecor] Service Registration Successful - Order #{registrationId}",
                    Body = EmailServiceRegistrationTemplate.RegistrationCreatedTemplate(
                        userName, registrationId.ToString(), packageName, serviceDate, nurseryName, status)
                }, CancellationToken.None);

                _logger.LogInformation("Sent registration-created email for RegistrationId={Id} to {Email}", registrationId, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send registration-created email for RegistrationId={Id}", registrationId);
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #2  Manager phê duyệt đơn
        // ──────────────────────────────────────────────────────────────────────────
        public async Task SendServiceRegistrationApprovedEmailAsync(int registrationId)
        {
            try
            {
                var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
                if (registration == null)
                {
                    _logger.LogWarning("SendServiceRegistrationApprovedEmail: Registration {Id} not found", registrationId);
                    return;
                }

                var toEmail = registration.User?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendServiceRegistrationApprovedEmail: No email for registration {Id}", registrationId);
                    return;
                }

                var userName = registration.User?.Username ?? "Customer";
                var packageName = registration.NurseryCareService?.CareServicePackage?.Name ?? "Care Service";
                var serviceDate = registration.ServiceDate?.ToString("dd/MM/yyyy") ?? "Not specified";
                var nurseryName = registration.NurseryCareService?.Nursery?.Name ?? "PlantDecor Nursery";
                var amount = (registration.NurseryCareService?.CareServicePackage?.UnitPrice ?? 0m)
                    .ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " VND";

                if (!registration.UserId.HasValue)
                {
                    _logger.LogWarning("SendServiceRegistrationApprovedEmail: No user id for registration {Id}", registrationId);
                    return;
                }

                var orderHistoryUrl = $"https://www.plantdecor.io.vn/orders/{registration.UserId.Value}";

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"[PlantDecor] Service Registration #{registrationId} Approved",
                    Body = EmailServiceRegistrationTemplate.RegistrationApprovedTemplate(
                        userName, registrationId.ToString(), packageName, serviceDate, amount, nurseryName, orderHistoryUrl)
                }, CancellationToken.None);

                _logger.LogInformation("Sent registration-approved email for RegistrationId={Id} to {Email}", registrationId, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send registration-approved email for RegistrationId={Id}", registrationId);
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #3  Manager từ chối đơn (final)
        // ──────────────────────────────────────────────────────────────────────────
        public async Task SendServiceRegistrationRejectedEmailAsync(int registrationId, string? rejectReason)
        {
            try
            {
                var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
                if (registration == null)
                {
                    _logger.LogWarning("SendServiceRegistrationRejectedEmail: Registration {Id} not found", registrationId);
                    return;
                }

                var toEmail = registration.User?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendServiceRegistrationRejectedEmail: No email for registration {Id}", registrationId);
                    return;
                }

                var userName = registration.User?.Username ?? "Customer";
                var packageName = registration.NurseryCareService?.CareServicePackage?.Name ?? "Care Service";

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"[PlantDecor] Service Registration #{registrationId} Update",
                    Body = EmailServiceRegistrationTemplate.RegistrationRejectedTemplate(
                        userName, registrationId.ToString(), packageName, rejectReason)
                }, CancellationToken.None);

                _logger.LogInformation("Sent registration-rejected email for RegistrationId={Id} to {Email}", registrationId, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send registration-rejected email for RegistrationId={Id}", registrationId);
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #4  Thanh toán thành công (Service order)
        // ──────────────────────────────────────────────────────────────────────────
        public async Task SendServicePaymentSuccessEmailAsync(int registrationId)
        {
            try
            {
                var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
                if (registration == null)
                {
                    _logger.LogWarning("SendServicePaymentSuccessEmail: Registration {Id} not found", registrationId);
                    return;
                }

                var toEmail = registration.User?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendServicePaymentSuccessEmail: No email for registration {Id}", registrationId);
                    return;
                }

                var userName = registration.User?.Username ?? "Customer";
                var packageName = registration.NurseryCareService?.CareServicePackage?.Name ?? "Care Service";
                var serviceDate = registration.ServiceDate?.ToString("dd/MM/yyyy") ?? "Not specified";
                var nurseryName = registration.NurseryCareService?.Nursery?.Name ?? "PlantDecor Nursery";
                var amount = (registration.NurseryCareService?.CareServicePackage?.UnitPrice ?? 0m)
                    .ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " VND";
                var paymentDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"[PlantDecor] Payment Successful - Care Service #{registrationId}",
                    Body = EmailServiceRegistrationTemplate.ServicePaymentSuccessTemplate(
                        userName, registrationId.ToString(), packageName, amount, paymentDate, serviceDate, nurseryName)
                }, CancellationToken.None);

                _logger.LogInformation("Sent service-payment-success email for RegistrationId={Id} to {Email}", registrationId, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send service-payment-success email for RegistrationId={Id}", registrationId);
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #5  Lịch chăm sóc được tạo + assign caretaker
        // ──────────────────────────────────────────────────────────────────────────
        public async Task SendServiceScheduleCreatedEmailAsync(int registrationId)
        {
            try
            {
                var registration = await _unitOfWork.ServiceRegistrationRepository.GetByIdWithDetailsAsync(registrationId);
                if (registration == null)
                {
                    _logger.LogWarning("SendServiceScheduleCreatedEmail: Registration {Id} not found", registrationId);
                    return;
                }

                var toEmail = registration.User?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendServiceScheduleCreatedEmail: No email for registration {Id}", registrationId);
                    return;
                }

                var userName = registration.User?.Username ?? "Customer";
                var packageName = registration.NurseryCareService?.CareServicePackage?.Name ?? "Care Service";
                var serviceDate = registration.ServiceDate?.ToString("dd/MM/yyyy") ?? "Not specified";
                var nurseryName = registration.NurseryCareService?.Nursery?.Name ?? "PlantDecor Nursery";
                var totalSessions = (registration.TotalSessions ?? 0).ToString();
                var caretakerName = registration.MainCaretaker?.Username;

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"[PlantDecor] Care Schedule Ready - Order #{registrationId}",
                    Body = EmailServiceRegistrationTemplate.ScheduleCreatedTemplate(
                        userName, registrationId.ToString(), packageName, serviceDate,
                        totalSessions, nurseryName, caretakerName)
                }, CancellationToken.None);

                _logger.LogInformation("Sent schedule-created email for RegistrationId={Id} to {Email}", registrationId, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send schedule-created email for RegistrationId={Id}", registrationId);
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────────────
        // #6  Caretaker Reassigned
        // ──────────────────────────────────────────────────────────────────────────
        public async Task SendCaretakerReassignedEmailAsync(int progressId)
        {
            try
            {
                var progress = await _unitOfWork.ServiceProgressRepository.GetByIdWithDetailsAsync(progressId);
                if (progress == null)
                {
                    _logger.LogWarning("SendCaretakerReassignedEmail: Progress {Id} not found", progressId);
                    return;
                }

                var registration = progress.ServiceRegistration;
                if (registration == null)
                {
                    _logger.LogWarning("SendCaretakerReassignedEmail: Registration not found for progress {Id}", progressId);
                    return;
                }

                var toEmail = registration.User?.Email;
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendCaretakerReassignedEmail: No email for registration {Id}", registration.Id);
                    return;
                }

                var userName = registration.User?.Username ?? "Customer";
                var packageName = registration.NurseryCareService?.CareServicePackage?.Name ?? "Care Service";
                var nurseryName = registration.NurseryCareService?.Nursery?.Name ?? "PlantDecor Nursery";
                var sessionDate = progress.TaskDate?.ToString("dd/MM/yyyy") ?? "Not specified";
                var shiftName = progress.Shift?.ShiftName;
                var caretakerName = progress.Caretaker?.Username;

                await _emailService.SendEmailAsync(new EmailRequest
                {
                    To = toEmail,
                    Subject = $"[PlantDecor] Staff Update - Care Session on {sessionDate}",
                    Body = EmailServiceRegistrationTemplate.CaretakerReassignedTemplate(
                        userName, registration.Id.ToString(), packageName, sessionDate, shiftName, nurseryName, caretakerName)
                }, CancellationToken.None);

                _logger.LogInformation("Sent caretaker-reassigned email for ProgressId={Id} to {Email}", progressId, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send caretaker-reassigned email for ProgressId={Id}", progressId);
                throw;
            }
        }
    }
}

