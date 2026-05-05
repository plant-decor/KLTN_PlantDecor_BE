
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using PlantDecor.API.Extensions;
using PlantDecor.API.Hubs;
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
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

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
            builder.Services.AddScoped<IOtpCacheService, OtpCacheService>();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.EnableDynamicJson();
            dataSourceBuilder.UseVector();
            var dataSource = dataSourceBuilder.Build();

            builder.Services.AddDbContext<PlantDecorContext>(options =>
                options.UseNpgsql(dataSource, o => o.UseVector()));

            //ADD SCOPED HERE
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Register Services
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<ITagService, TagService>();
            builder.Services.AddScoped<IPlantService, PlantService>();
            builder.Services.AddScoped<IPlantGuideService, PlantGuideService>();
            builder.Services.AddScoped<IShopSearchService, ShopSearchService>();

            builder.Services.AddScoped<IPlantInstanceService, PlantInstanceService>();
            builder.Services.AddScoped<IMaterialService, MaterialService>();
            builder.Services.AddScoped<ICommonPlantService, CommonPlantService>();
            builder.Services.AddScoped<IPlantComboService, PlantComboService>();
            builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
            builder.Services.AddScoped<IUserService, UserService>();
            //builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IEmailService, ResendEmailService>();
            builder.Services.AddScoped<IEmailBackgroundJobService, EmailBackgroundJobService>();
            builder.Services.AddScoped<IOrderBackgroundJobService, OrderBackgroundJobService>();
            builder.Services.AddScoped<ITokenCleanupService, TokenCleanupService>();
            builder.Services.AddScoped<IPaymentTimeoutService, PaymentTimeoutService>();
            builder.Services.AddScoped<IUserBehaviorLogService, UserBehaviorLogService>();
            builder.Services.AddScoped<IUserPreferenceService, UserPreferenceService>();
            builder.Services.AddScoped<IUserPlantService, UserPlantService>();
            builder.Services.AddScoped<ICareReminderService, CareReminderService>();
            builder.Services.AddScoped<IChatService, ChatService>();

            // Cart & Wishlist
            builder.Services.AddScoped<ICartService, CartService>();
            builder.Services.AddScoped<IWishlistService, WishlistService>();

            // Order & Invoice & Payment
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<INurseryOrderService, NurseryOrderService>();
            builder.Services.AddScoped<IReturnTicketService, ReturnTicketService>();
            builder.Services.AddScoped<IReturnTicketManagerService, ReturnTicketManagerService>();

            // Nursery Management APIs
            builder.Services.AddScoped<INurseryService, NurseryService>();
            builder.Services.AddScoped<INurseryMaterialService, NurseryMaterialService>();

            // Care Service APIs
            builder.Services.AddScoped<ICareServicePackageService, CareServicePackageService>();
            builder.Services.AddScoped<INurseryCareServiceService, NurseryCareServiceService>();
            builder.Services.AddScoped<IServiceRegistrationService, ServiceRegistrationService>();
            builder.Services.AddScoped<IServiceProgressService, ServiceProgressService>();
            builder.Services.AddScoped<IServiceCareBackgroundJobService, ServiceCareBackgroundJobService>();
            builder.Services.AddScoped<IServiceRatingService, ServiceRatingService>();
            builder.Services.AddScoped<ISpecializationService, SpecializationService>();
            builder.Services.AddScoped<IShiftService, ShiftService>();
            builder.Services.AddScoped<IDepositPolicyService, DepositPolicyService>();
            builder.Services.AddScoped<IDesignTemplateService, DesignTemplateService>();
            builder.Services.AddScoped<IDesignTemplateTierService, DesignTemplateTierService>();
            builder.Services.AddScoped<INurseryDesignTemplateService, NurseryDesignTemplateService>();
            builder.Services.AddScoped<IDesignRegistrationService, DesignRegistrationService>();
            builder.Services.AddScoped<IDesignTaskService, DesignTaskService>();

            // PlantInstance Management APIs
            builder.Services.AddScoped<IPlantInstanceService, PlantInstanceService>();

            // Embedding & AI Search Services
            builder.Services.AddScoped<IEmbeddingTextSerializer, EmbeddingTextSerializer>();
            builder.Services.AddScoped<IEmbeddingTextPreprocessor, EmbeddingTextPreprocessor>();
            builder.Services.AddScoped<IEmbeddingChunker, EmbeddingChunker>();
            builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
            builder.Services.AddScoped<IAISearchService, AISearchService>();
            builder.Services.AddScoped<IRoomDesignService, RoomDesignService>();
            builder.Services.AddScoped<IRoomImageService, RoomImageService>();
            builder.Services.AddHttpClient<ILayoutDesignImageGenerationService, LayoutDesignImageGenerationService>();
            builder.Services.AddScoped<IEmbeddingBackgroundJobService, EmbeddingBackgroundJobService>();
            builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();
            builder.Services.AddScoped<IPolicyKnowledgeService, PolicyKnowledgeService>();
            builder.Services.AddScoped<IPolicyContentService, PolicyContentService>();

            builder.Services.AddCors(options =>
            {
                // Policy cho Development
                options.AddPolicy("Development",
                    policy => policy
                        .WithOrigins(
                            "https://localhost:3000",         // React dev
                            "http://localhost:3000",         // React dev
                            "http://localhost:5173",         // Vite
                            "http://localhost:5500",         // Live Server
                            "http://localhost:7180",         // API dev port
                            "https://localhost:7180",        // API dev port HTTPS
                            "http://127.0.0.1:3000",         // React dev (127.0.0.1)
                            "http://127.0.0.1:5173",         // Vite (127.0.0.1)
                            "http://127.0.0.1:5500",         // Live Server (127.0.0.1)
                            "http://127.0.0.1:7180",         // API dev (127.0.0.1)
                            "https://127.0.0.1:7180",        // API dev HTTPS (127.0.0.1)
                            "null",                          // file:// local HTML
                            "https://www.plantdecor.io.vn",
                            "https://api.plantdecor.io.vn"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());

                // Policy cho Production
                options.AddPolicy("Production",
                    policy => policy
                        .WithOrigins(
                    "https://www.plantdecor.io.vn",
                    "https://plantdecor.io.vn",
                     "http://localhost:3000",
                    "https://api.plantdecor.io.vn"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests; // Too Many Requests


                // 1. Global: Token Bucket cho toàn bộ API (theo IP)
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // BỎ QUA RATE LIMIT CHO HANGFIRE DASHBOARD VÀ SWAGGER (Bỏ cả swagger vì có thể tốn nhiều request để lấy các file như .js, .css)
                    var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
                    if (path.StartsWith("/hangfire")
                        || path.StartsWith("/swagger")
                        || path.StartsWith("/health")
                        || path.StartsWith("/api/Authentication"))
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

            var jwtSigningKey = builder.Configuration["JwtSettings:Key"]
                ?? throw new InvalidOperationException("JwtSettings:Key is not configured.");

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
                       IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSigningKey))
                   };
                   // map "Role" claim về role cho ASP.NET Core
                   options.TokenValidationParameters.RoleClaimType = "Role";

                   // ✅ SignalR Support: Read token from query string for WebSocket connections
                   options.Events = new JwtBearerEvents
                   {
                       OnMessageReceived = context =>
                       {
                           // 1. Ưu tiên lấy từ COOKIE
                           var token = context.Request.Cookies["accessToken"];

                           if (!string.IsNullOrEmpty(token))
                           {
                               context.Token = token;
                               return Task.CompletedTask;
                           }

                           // 2. SignalR (query string)
                           var accessToken = context.Request.Query["access_token"];
                           var path = context.HttpContext.Request.Path;

                           // If the request is for SignalR hub and token is in query string
                           if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                           {
                               context.Token = accessToken;
                               return Task.CompletedTask;
                           }

                           //  3. Fallback: Authorization header (Swagger)
                           var authHeader = context.Request.Headers["Authorization"].ToString();

                           if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
                           {
                               context.Token = authHeader.Substring("Bearer ".Length).Trim();
                           }
                           return Task.CompletedTask;
                       }
                   };
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

            // SignalR for realtime features with extended options
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
            });

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
            //    if (app.Environment.IsDevelopment() )
            //   {
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHangfireDashboard(options: new DashboardOptions
            {
                Authorization = [],
                DarkModeEnabled = true
            });
            // }
            // dùng để lấy đúng IP của client khi có reverse proxy (nginx, load balancer) ở phía trước,
            // nếu không có thì sẽ bị lỗi do tất cả request đều có cùng 1 IP (IP của proxy)
            app.UseForwardedHeaders();
            // đặt ở đây vì muốn nó bắt được tất cả exception, kể cả exception do authentication
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseHttpsRedirection();
            app.UseRouting();
            // Tự động chọn theo môi trường
            if (app.Environment.IsDevelopment())
                app.UseCors("Development");
            else
                app.UseCors("Production");
            //app.UseRateLimiter();
            app.UseAuthentication();
            // để sau authentication thay vì ở đầu pipeline để tránh việc phải check security stamp cho các request không cần authentication (như swagger, health check, static files...)
            app.UseMiddleware<SecurityStampValidationMiddleware>();
            app.UseAuthorization();
            app.MapControllers();
            // Map SignalR hubs
            app.MapHub<ChatHub>("/hubs/chat");
            app.MapHealthChecks("/health");
            app.RegisterRecurringJobs();
            app.Run();
        }
    }
}
