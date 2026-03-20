using PlantDecor.BusinessLogicLayer.Exceptions;
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
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (ForbiddenException ex) // Custom exception
            {
                _logger.LogWarning(ex, "Forbidden access: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status404NotFound, ex.Message);
            }
            catch (ConflictException ex)
            {
                _logger.LogWarning(ex, "Conflict occurred: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status409Conflict, ex.Message);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning(ex, "Bad request: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status400BadRequest, ex.Message);
            }
            catch (SecurityStampMismatchException ex)
            {
                _logger.LogWarning(ex, "Security stamp mismatch: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status401Unauthorized, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Path}", context.Request.Path);
                await HandleException(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later");
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
}
