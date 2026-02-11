using System.Text.Json;

namespace PlantDecor.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (ForbiddenAccessException ex) // Custom exception
            {
                _logger.LogWarning(ex, "Forbidden access: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Bad request: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status500InternalServerError, "Internal Server Error");
            }
        }

        private async Task HandleException(HttpContext context,
            int statusCode, string message)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Response has already started, cannot write error response");
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                success = false,
                statusCode,
                message,
                traceId = context.TraceIdentifier // Để trace log
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsJsonAsync(response, options);
        }
    }

    // Custom Exceptions
    public sealed class ForbiddenAccessException : Exception
    {
        public ForbiddenAccessException() : base("You do not have permission to access this resource") { }
        public ForbiddenAccessException(string message) : base(message) { }
    }
}
