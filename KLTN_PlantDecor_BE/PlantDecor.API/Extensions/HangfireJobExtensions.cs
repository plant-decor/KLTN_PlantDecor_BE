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
        /// Configure global Hangfire settings for retry policies
        /// </summary>
        public static void ConfigureHangfireRetryPolicy(this IGlobalConfiguration configuration)
        {
            // Retry failed jobs automatically up to 3 times
            configuration.UseFilter(new AutomaticRetryAttribute
            {
                Attempts = 3,
                OnAttemptsExceeded = AttemptsExceededAction.Delete
            });
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
        }
    }
}
