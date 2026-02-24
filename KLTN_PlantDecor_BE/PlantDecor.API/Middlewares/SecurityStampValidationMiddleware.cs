using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Middlewares
{
    public class SecurityStampValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SecurityStampValidationMiddleware> _logger;

        public SecurityStampValidationMiddleware(RequestDelegate next, ILogger<SecurityStampValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        // Danh sách endpoint không cần check SecurityStamp
        private static readonly string[] _excludedPaths = new[]
        {
        "/swagger",
        "/health",
        "/api/public",
        "/favicon.ico",
        "/css",
        "/js",
        "/images",
        "/api/Authentication/login",
        "/api/Authentication/register",
        "/api/Authentication/refreshToken",
        "/api/Authentication/forgot-password",
        "/api/Authentication/reset-password",
        "/api/Authentication/verify-email",
        "/api/Authentication/confirm-email",
        "/api/Authentication/login-google"

        };

        public async Task InvokeAsync(HttpContext context, ISecurityStampCacheService securityStampCacheService)
        {
            //await using var scope = _serviceProvider.CreateAsyncScope();
            //var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            // var path = context.Request.Path.Value?.ToLower();

            // Bỏ qua những route không cần kiểm tra
            if (_excludedPaths.Any(excluded => context.Request.Path.StartsWithSegments(excluded, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Chỉ validate cho requests đã authenticated
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var securityStampClaim = context.User.FindFirst("SecurityStamp")?.Value;

                if (!string.IsNullOrEmpty(userIdClaim) && !string.IsNullOrEmpty(securityStampClaim) && int.TryParse(userIdClaim, out int userId))
                {
                    // Check Redis trước (~1ms), cache miss mới query DB
                    var isValid = await securityStampCacheService.ValidateSecurityStampAsync(userId, securityStampClaim);
                    if (!isValid)
                    {
                        _logger.LogWarning(
                           "SecurityStamp mismatch for UserId: {UserId}, Path: {Path}",
                           userId, context.Request.Path);

                        // Invalid security stamp -> force re-login
                        throw new SecurityStampMismatchException();
                    }
                }
            }

            await _next(context);
        }
    }
}
