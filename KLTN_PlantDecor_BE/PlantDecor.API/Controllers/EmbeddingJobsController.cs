using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PlantDecor.API.Extensions;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.Constants;

namespace PlantDecor.API.Controllers
{
    [Route("api/embedding-jobs")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class EmbeddingJobsController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IConfiguration _configuration;

        public EmbeddingJobsController(
            IBackgroundJobClient backgroundJobClient,
            IConfiguration configuration)
        {
            _backgroundJobClient = backgroundJobClient;
            _configuration = configuration;
        }

        [HttpPost("backfill/all")]
        public IActionResult BackfillAll([FromQuery] int? batchSize = null)
        {
            NpgsqlConnection.ClearAllPools();
            var normalizedBatchSize = NormalizeBatchSize(batchSize);
            var jobId = _backgroundJobClient.EnqueueEmbeddingBackfillAll(normalizedBatchSize);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Queued embedding backfill for all entity types",
                Payload = new
                {
                    JobId = jobId,
                    BatchSize = normalizedBatchSize,
                    ChunkingEnabled = IsChunkingEnabled(),
                    EntityTypes = EmbeddingEntityTypes.AllTypes
                }
            });
        }

        [HttpPost("backfill/{entityType}")]
        public IActionResult BackfillByEntityType(string entityType, [FromQuery] int? batchSize = null)
        {
            NpgsqlConnection.ClearAllPools();
            if (!TryResolveEntityType(entityType, out var resolvedEntityType))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = $"Invalid entityType '{entityType}'. Valid values: {string.Join(", ", EmbeddingEntityTypes.AllTypes)}"
                });
            }

            var normalizedBatchSize = NormalizeBatchSize(batchSize);
            var jobId = _backgroundJobClient.EnqueueEmbeddingBackfillByType(resolvedEntityType!, normalizedBatchSize);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Queued embedding backfill for entity type {resolvedEntityType}",
                Payload = new
                {
                    JobId = jobId,
                    EntityType = resolvedEntityType,
                    BatchSize = normalizedBatchSize,
                    ChunkingEnabled = IsChunkingEnabled()
                }
            });
        }

        private bool IsChunkingEnabled()
            => _configuration.GetValue<bool>("EmbeddingProcessingSettings:EnableChunking", false);

        private int NormalizeBatchSize(int? requestedBatchSize)
        {
            var defaultBatchSize = _configuration.GetValue<int>("EmbeddingBackfillSettings:DefaultBatchSize", 50);
            var maxBatchSize = _configuration.GetValue<int>("EmbeddingBackfillSettings:MaxBatchSize", 100);
            var safeMaxBatchSize = Math.Clamp(maxBatchSize, 1, 500);

            var effectiveBatchSize = requestedBatchSize ?? defaultBatchSize;
            return Math.Clamp(effectiveBatchSize, 1, safeMaxBatchSize);
        }

        private static bool TryResolveEntityType(string entityType, out string? resolvedEntityType)
        {
            resolvedEntityType = EmbeddingEntityTypes.AllTypes
                .FirstOrDefault(t => t.Equals(entityType, StringComparison.OrdinalIgnoreCase));

            return !string.IsNullOrWhiteSpace(resolvedEntityType);
        }
    }
}
