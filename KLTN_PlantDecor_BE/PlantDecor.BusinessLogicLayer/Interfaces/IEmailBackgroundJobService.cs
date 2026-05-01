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
    }
}
