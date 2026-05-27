namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IEmailBackgroundJobService
    {
        /// <summary>
        /// Send order success email in background
        /// </summary>
        /// <param name="orderId">Order ID to send email for</param>
        Task SendOrderSuccessEmailAsync(int orderId);

        /// <summary>
        /// Send verification email in background after registration
        /// </summary>
        /// <param name="email">User email to send verification link to</param>
        Task SendVerificationEmailAsync(string email);

        /// <summary>
        /// Send OTP verification email in background after registration
        /// </summary>
        /// <param name="email">User email to send OTP verification to</param>
        Task SendOtpEmailVerificationAsync(string email);

        /// <summary>
        /// Gửi email xác nhận tạo đơn đăng ký dịch vụ thành công
        /// </summary>
        Task SendServiceRegistrationCreatedEmailAsync(int registrationId);

        /// <summary>
        /// Gửi email thông báo đơn đăng ký dịch vụ được phê duyệt — nhắc thanh toán
        /// </summary>
        Task SendServiceRegistrationApprovedEmailAsync(int registrationId);

        /// <summary>
        /// Gửi email thông báo đơn đăng ký dịch vụ bị từ chối (final)
        /// </summary>
        Task SendServiceRegistrationRejectedEmailAsync(int registrationId, string? rejectReason);

        /// <summary>
        /// Gửi email thông báo thanh toán thành công cho đơn dịch vụ chăm sóc cây
        /// </summary>
        Task SendServicePaymentSuccessEmailAsync(int registrationId);

        /// <summary>
        /// Gửi email thông báo lịch chăm sóc đã được thiết lập (sau khi thanh toán)
        /// </summary>
        Task SendServiceScheduleCreatedEmailAsync(int registrationId);

        /// <summary>
        /// Gửi email thông báo khi nhân viên chăm sóc được thay đổi cho một ca làm việc
        /// </summary>
        Task SendCaretakerReassignedEmailAsync(int progressId);
    }
}
