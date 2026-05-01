using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
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
        /// Enqueue OTP verification email with automatic retry on failure
        /// </summary>
        /// <param name="backgroundJobClient">Hangfire background job client</param>
        /// <param name="email">User email to send OTP verification to</param>
        /// <returns>Job ID</returns>
        public static string EnqueueOtpEmailVerification(
            this IBackgroundJobClient backgroundJobClient,
            string email)
        {
            return backgroundJobClient.Enqueue<IEmailBackgroundJobService>(
                service => service.SendOtpEmailVerificationAsync(email));
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

        #region Embedding Jobs

        /// <summary>
        /// Enqueue CommonPlant embedding processing
        /// </summary>
        public static string EnqueueCommonPlantEmbedding(
            this IBackgroundJobClient backgroundJobClient,
            CommonPlantEmbeddingDto dto,
            Guid entityId,
            string entityType)
        {
            return backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                service => service.ProcessCommonPlantEmbeddingAsync(dto, entityId, entityType));
        }

        /// <summary>
        /// Enqueue PlantInstance embedding processing
        /// </summary>
        public static string EnqueuePlantInstanceEmbedding(
            this IBackgroundJobClient backgroundJobClient,
            PlantInstanceEmbeddingDto dto,
            Guid entityId,
            string entityType)
        {
            return backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                service => service.ProcessPlantInstanceEmbeddingAsync(dto, entityId, entityType));
        }

        /// <summary>
        /// Enqueue NurseryPlantCombo embedding processing
        /// </summary>
        public static string EnqueueNurseryPlantComboEmbedding(
            this IBackgroundJobClient backgroundJobClient,
            NurseryPlantComboEmbeddingDto dto,
            Guid entityId,
            string entityType)
        {
            return backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                service => service.ProcessNurseryPlantComboEmbeddingAsync(dto, entityId, entityType));
        }

        /// <summary>
        /// Enqueue NurseryMaterial embedding processing
        /// </summary>
        public static string EnqueueNurseryMaterialEmbedding(
            this IBackgroundJobClient backgroundJobClient,
            NurseryMaterialEmbeddingDto dto,
            Guid entityId,
            string entityType)
        {
            return backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                service => service.ProcessNurseryMaterialEmbeddingAsync(dto, entityId, entityType));
        }

        /// <summary>
        /// Enqueue backfill jobs for all embedding entity types in batches
        /// </summary>
        public static string EnqueueEmbeddingBackfillAll(
            this IBackgroundJobClient backgroundJobClient,
            int batchSize)
        {
            return backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                service => service.QueueBackfillAllAsync(batchSize));
        }

        /// <summary>
        /// Enqueue backfill jobs for a specific embedding entity type in batches
        /// </summary>
        public static string EnqueueEmbeddingBackfillByType(
            this IBackgroundJobClient backgroundJobClient,
            string entityType,
            int batchSize)
        {
            return backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                service => service.QueueBackfillByEntityTypeAsync(entityType, batchSize));
        }

        #endregion

        /// <summary>
        /// Enqueue service care schedule generation after a Service order is paid
        /// </summary>
        public static string EnqueueServiceScheduleGeneration(
            this IBackgroundJobClient backgroundJobClient,
            int serviceRegistrationId)
        {
            return backgroundJobClient.Enqueue<IServiceCareBackgroundJobService>(
                service => service.GenerateServiceScheduleAsync(serviceRegistrationId));
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

            // Auto-complete orders in PendingConfirmation for at least 3 days (run daily)
            recurringJobManager.AddOrUpdate<IOrderBackgroundJobService>(
                "auto-complete-pending-confirmation-orders",
                service => service.AutoCompletePendingConfirmationOrdersAsync(),
                "0 0 * * *");
        }
    }
}
