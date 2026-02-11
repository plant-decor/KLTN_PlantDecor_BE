
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });
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

            builder.Services.AddDbContext<PlantDecorContext>(options =>
 options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            //ADD SCOPED HERE
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

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
                    var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault() // ưu tiên vì trường hợp dùng nginx hay loadbalancer thì sẽ chung 1 ip nên ko dùng kiểu ipAddress đc
         ?? context.Connection.RemoteIpAddress?.ToString()
         ?? "unknown";
                    return RateLimitPartition.GetTokenBucketLimiter(ip, _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = 50,
                        TokensPerPeriod = 10, // Cấp phát 10 token cho mỗi chu kỳ
                        ReplenishmentPeriod = TimeSpan.FromSeconds(10), // Mỗi 10 giây cấp phát lại token
                        AutoReplenishment = true // Tự động cấp phát lại token
                    });
                });

                // 2. Strict: Cho endpoints nhạy cảm (login, register, forgot-password)
                options.AddPolicy("auth-strict", context =>
                {
                    var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                             ?? context.Connection.RemoteIpAddress?.ToString()
                             ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(3),
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
                options.HeartbeatInterval = TimeSpan.FromSeconds(10);
                options.ServerTimeout = TimeSpan.FromSeconds(30);
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

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseCors("AllowAllOrigins");
            app.UseRateLimiter();
            app.UseAuthentication();
            //        app.UseMiddleware<SecurityStampValidationMiddleware>();
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/health");
            app.Run();
        }
    }
}
