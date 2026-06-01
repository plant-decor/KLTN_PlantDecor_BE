namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IEmailBackgroundJobService
    {
        Task SendOrderSuccessEmailAsync(int orderId);
        Task SendVerificationEmailAsync(string email);
        Task SendOtpEmailVerificationAsync(string email);

        Task SendServiceRegistrationCreatedEmailAsync(int registrationId);
        Task SendServiceRegistrationApprovedEmailAsync(int registrationId);
        Task SendServiceRegistrationRejectedEmailAsync(int registrationId, string? rejectReason);
        Task SendServicePaymentSuccessEmailAsync(int registrationId);
        Task SendServiceScheduleCreatedEmailAsync(int registrationId);
        Task SendCaretakerReassignedEmailAsync(int progressId);

        Task SendDesignRegistrationCreatedEmailAsync(int registrationId);
        Task SendDesignRegistrationApprovedEmailAsync(int registrationId);
        Task SendDesignRegistrationRejectedEmailAsync(int registrationId, string? rejectReason);
        Task SendDesignCaretakerAssignedEmailAsync(int registrationId);
        Task SendDesignRegistrationCancelledEmailAsync(int registrationId, string? cancelReason);
    }
}
