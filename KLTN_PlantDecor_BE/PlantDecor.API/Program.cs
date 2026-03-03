
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlantDecor.API.Middlewares;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Services;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.UnitOfWork;
using Resend;
using System.Globalization;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace PlantDecor.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            builder.Services.AddHealthChecks();

            //Settings for AddControllers

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // Disable automatic model state validation to return custom error responses
            builder.Services.AddControllers().ConfigureApiBehaviorOptions(options => { options.SuppressModelStateInvalidFilter = true; });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "PlantDecor API",
                    Version = "v1",
                    Description = "API for Plant Decoration System",
                    Contact = new OpenApiContact
                    {
                        Name = "Development Team",
                        Email = "thangndse183829@fpt.edu.vn"
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

                // JWT Authentication in Swagger
                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Description = @"Enter Your JWT Token in this field",
                    Name = "JWT Authentication",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = JwtBearerDefaults.AuthenticationScheme
                            },
                        },
                        new List<string>()
                    }
                });
            });

            var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

            builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "PlantDecor_";
            });

            // Redis Service Registration
            builder.Services.AddScoped<ICacheService, RedisCacheService>();
            builder.Services.AddScoped<ISecurityStampCacheService, SecurityStampCacheService>();

            builder.Services.AddDbContext<PlantDecorContext>(options =>
 options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            //ADD SCOPED HERE
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Register Services
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IPlantService, PlantService>();
            builder.Services.AddScoped<IPlantInstanceService, PlantInstanceService>();
            builder.Services.AddScoped<IInventoryService, InventoryService>();
            builder.Services.AddScoped<IPlantInventoryService, PlantInventoryService>();
            builder.Services.AddScoped<IPlantComboService, PlantComboService>();
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IEmailService, EmailService>();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // Too Many Requests


                // 1. Global: Token Bucket cho toàn bộ API (theo IP)
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // BỎ QUA RATE LIMIT CHO HANGFIRE DASHBOARD VÀ SWAGGER (Bỏ cả swagger vì có thể tốn nhiều request để lấy các file như .js, .css)
                    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                    if (path.StartsWith("/hangfire") || path.StartsWith("/swagger") || path.StartsWith("/health"))
                    {
                        return RateLimitPartition.GetNoLimiter("BypassLimiter");
                    }

                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                    return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = builder.Configuration.GetValue<int>("RateLimiting:Global:TokenLimit", 100),
                        TokensPerPeriod = builder.Configuration.GetValue<int>("RateLimiting:Global:TokensPerPeriod", 20), // Cấp phát 20 token cho mỗi chu kỳ
                        ReplenishmentPeriod = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:Global:ReplenishmentPeriodSeconds", 10)), // Mỗi 10 giây cấp phát lại token
                        AutoReplenishment = true // Tự động cấp phát lại token
                    });
                });

                // 2. Strict: Cho endpoints nhạy cảm (login, register, forgot-password)
                options.AddPolicy("auth-strict", context =>
                {
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:AuthStrict:PermitLimit", 5),
                        Window = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("RateLimiting:AuthStrict:WindowMinutes", 3)), // Trong 3 phút chỉ cho phép 5 request
                        SegmentsPerWindow = builder.Configuration.GetValue<int>("RateLimiting:AuthStrict:SegmentsPerWindow", 6), // Chia cửa sổ thành 6 đoạn (mỗi đoạn 30 giây) để phân phối lại request tốt hơn
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
                });

                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);
                    }

                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        statusCode = 429,
                        message = "Too many requests. Please try again later.",
                        traceId = context.HttpContext.TraceIdentifier
                    });

                };

            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
               .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
               {
                   options.RequireHttpsMetadata = false;
                   options.SaveToken = true;
                   options.TokenValidationParameters = new TokenValidationParameters
                   {
                       ValidateIssuer = true,
                       ValidateAudience = true,
                       ValidateLifetime = true,
                       ValidateIssuerSigningKey = true,
                       ClockSkew = TimeSpan.Zero, // Disable the default clock skew of 5 minutes
                       ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
                       ValidAudience = builder.Configuration["JwtSettings:Audience"],
                       IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:Key"]))
                   };
                   // map "Role" claim về role cho ASP.NET Core
                   options.TokenValidationParameters.RoleClaimType = "Role";
               });
            builder.Services.AddHttpClient();
            builder.Services.AddOptions();

            // Cấu hình để lấy đúng IP của client khi có reverse proxy (nginx, load balancer) ở phía trước,
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto;

                // Nếu Nginx cùng máy (Docker compose hoặc cùng server) thì không cần thêm
                // Nếu Nginx ở máy khác, thêm IP của nó:
                // options.KnownProxies.Add(System.Net.IPAddress.Parse("your-nginx-ip"));

                // Hoặc nếu dùng Docker network:
                // options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(
                //     System.Net.IPAddress.Parse("172.16.0.0"), 12));
            });
            builder.Services.AddHttpClient<ResendClient>();
            builder.Services.Configure<ResendClientOptions>(o =>
            {
                o.ApiToken = builder.Configuration["Resend:ApiKey"]!;
            });

            builder.Services.AddTransient<IResend, ResendClient>();
            builder.Services.AddHangfire(config =>
            {
                config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

                // Configure automatic retry policy for failed jobs
                config.UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = 3, // Retry up to 3 times
                    DelaysInSeconds = new[] { 60, 300, 900 }, // 1min, 5min, 15min
                    OnAttemptsExceeded = AttemptsExceededAction.Delete
                });
            });

            // Kiểm tra các job scheduled (đã lên lịch) mỗi 1 giây.
            builder.Services.AddHangfireServer(options =>
            {
                options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                options.WorkerCount = 5; // Number of concurrent job executions
                options.HeartbeatInterval = TimeSpan.FromSeconds(30); // Kiểm tra tình trạng của worker mỗi 30 giây
                options.ServerTimeout = TimeSpan.FromMinutes(5); // Nếu worker không phản hồi trong 5 phút, coi như bị treo và sẽ được đánh dấu là failed để có thể retry lại
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseHangfireDashboard(options: new DashboardOptions
                {
                    Authorization = [],
                    DarkModeEnabled = true
                });
            }
            // dùng để lấy đúng IP của client khi có reverse proxy (nginx, load balancer) ở phía trước,
            // nếu không có thì sẽ bị lỗi do tất cả request đều có cùng 1 IP (IP của proxy)
            app.UseForwardedHeaders();
            // đặt ở đây vì muốn nó bắt được tất cả exception, kể cả exception do authentication
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAllOrigins");
            app.UseRateLimiter();
            app.UseAuthentication();
            // để sau authentication thay vì ở đầu pipeline để tránh việc phải check security stamp cho các request không cần authentication (như swagger, health check, static files...)
            //app.UseMiddleware<SecurityStampValidationMiddleware>();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");
            app.Run();
        }
    }
}
