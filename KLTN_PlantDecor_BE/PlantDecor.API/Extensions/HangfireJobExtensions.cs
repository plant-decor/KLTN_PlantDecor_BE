using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Extensions
{
    public static class HangfireJobExtensions
    {
        /// <summary>
        /// Enqueue order success email with automatic retry on failure
        /// </summary>
        /// <param name="backgroundJobClient">Hangfire background job client</param>
        /// <param name="orderId">Order ID to send email for</param>
        /// <returns>Job ID</returns>
        public static string EnqueueOrderSuccessEmail(
            this IBackgroundJobClient backgroundJobClient,
            int orderId)
        {
            return backgroundJobClient.Enqueue<IEmailBackgroundJobService>(
                service => service.SendOrderSuccessEmailAsync(orderId));
        }

        /// <summary>
        /// Enqueue verification email with automatic retry on failure
        /// </summary>
        /// <param name="backgroundJobClient">Hangfire background job client</param>
        /// <param name="email">User email to send verification link to</param>
        /// <returns>Job ID</returns>
        public static string EnqueueVerificationEmail(
            this IBackgroundJobClient backgroundJobClient,
            string email)
        {
            return backgroundJobClient.Enqueue<IEmailBackgroundJobService>(
                service => service.SendVerificationEmailAsync(email));
        }

        /// <summary>
        /// Schedule order success email to be sent after a delay
        /// </summary>
        /// <param name="backgroundJobClient">Hangfire background job client</param>
        /// <param name="orderId">Order ID to send email for</param>
        /// <param name="delay">Delay before sending email</param>
        /// <returns>Job ID</returns>
        public static string ScheduleOrderSuccessEmail(
            this IBackgroundJobClient backgroundJobClient,
            int orderId,
            TimeSpan delay)
        {
            return backgroundJobClient.Schedule<IEmailBackgroundJobService>(
                service => service.SendOrderSuccessEmailAsync(orderId),
                delay);
        }

        /// <summary>
        /// Enqueue order delivery processing - check strategy and create RemainingBalance invoice if needed
        /// </summary>
        /// <param name="backgroundJobClient">Hangfire background job client</param>
        /// <param name="orderId">Order ID to process</param>
        /// <returns>Job ID</returns>
        public static string EnqueueOrderDeliveryProcessing(
            this IBackgroundJobClient backgroundJobClient,
            int orderId)
        {
            return backgroundJobClient.Enqueue<IOrderBackgroundJobService>(
                service => service.ProcessOrderDeliveryAsync(orderId));
        }

        /// <summary>
        /// Register all recurring Hangfire jobs
        /// </summary>
        public static void RegisterRecurringJobs(this IApplicationBuilder app)
        {
            var recurringJobManager = app.ApplicationServices.GetRequiredService<IRecurringJobManager>();

            // Delete revoked refresh tokens every 10 minutes
            recurringJobManager.AddOrUpdate<ITokenCleanupService>(
                "cleanup-revoked-refresh-tokens",
                service => service.CleanupRevokedTokensAsync(),
                "*/10 * * * *");

            // Mark expired pending payments/transactions every minute
            recurringJobManager.AddOrUpdate<IPaymentTimeoutService>(
                "process-expired-payments",
                service => service.ProcessExpiredPaymentsAsync(),
                "*/1 * * * *");
        }
    }
}
