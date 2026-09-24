using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OrderingSystem.Application.Interfaces.Auth;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.Application.Interfaces.Bills;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.Application.Interfaces.Data;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.Application.Services;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Authentication;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Infrastructure.ExternalServices.Notifications;
using OrderingSystem.Infrastructure.Notifications;
using OrderingSystem.Infrastructure.Queries;
using OrderingSystem.Infrastructure.Repositories;
using OrderingSystem.Infrastructure.Seeding;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Caching ──────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── HTTP Logging ─────────────────────────────────────────────────────────
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.All;
    logging.MediaTypeOptions.AddText("application/json");
    logging.CombineLogs = true;
});

// ── Controllers & JSON ───────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Rate Limiting ────────────────────────────────────────────────────────
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("DynamicRestaurantPolicy", httpContext =>
    {
        var sessionId = httpContext.User.FindFirst("DeviceSessionId")?.Value;
        var hasDeviceSession = !string.IsNullOrWhiteSpace(sessionId);

        if (!hasDeviceSession && httpContext.Request.Cookies.TryGetValue("DeviceSessionId", out var cookieValue))
        {
            sessionId = cookieValue;
            hasDeviceSession = true;
        }

        if (hasDeviceSession && !string.IsNullOrWhiteSpace(sessionId))
        {
            return RateLimitPartition.GetFixedWindowLimiter(sessionId, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 150,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        }

        // 3. Anonymous Request: IP-based partition for shared Restaurant Wi-Fi
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.TraceIdentifier;

        return RateLimitPartition.GetTokenBucketLimiter(ip, _ =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 1000, // Maximum initial burst capacity for the shared IP
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 150, // Replenishes 150 requests every 10 seconds (900/min sustained)
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50
            });
    });

    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.TraceIdentifier;

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });
});

// ── Swagger with JWT Bearer ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter your JWT token.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

// ── Database Context ──────────────────────────────────────────────────────
// Switched to AddDbContextPool for connection multiplexing
builder.Services.AddDbContextPool<OrderingSystemDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b =>
        {
            b.MigrationsAssembly(typeof(OrderingSystemDbContext).Assembly.FullName);
            b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(2),
                errorCodesToAdd: null
            );
        }));

// ── Health Checks ──────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderingSystemDbContext>();

// ── Dependency Injections ──────────────────────────────────────────────────
builder.Services.AddSingleton<IDatabaseErrorMapper, PostgresErrorMapper>();
builder.Services.AddExceptionHandler<OrderingSystem.WebApi.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHostedService<OrderingSystem.BackgroundServices.ZombieSessionCleanupWorker>();

builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<ISessionCommandService, SessionCommandService>();
builder.Services.AddScoped<ITableSessionRepository, TableSessionRepository>();
builder.Services.AddScoped<ITableSessionQuery, TableSessionQuery>();
builder.Services.AddScoped<IDeviceSessionRepository, DeviceSessionRepository>();
builder.Services.AddScoped<IDeviceSessionQuery, DeviceSessionQuery>();
builder.Services.AddScoped<ITableRepository, TableRepository>();
builder.Services.AddScoped<ITableCommandService, TableCommandService>();
builder.Services.AddScoped<ITableQuery, TableQuery>();
builder.Services.AddScoped<IMenueItemRepository, MenuItemRepsository>();
builder.Services.AddScoped<IMenueItemCommandService, MenueItemCommandService>();
builder.Services.AddScoped<IMenueItemQuery, MenuItemQuery>();
builder.Services.AddScoped<ICategoryCommandService, CategoryCommandService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryQuery, CategoryQuery>();
builder.Services.AddScoped<IAuthCommandService, AuthCommandService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderCommandService, OrderCommandService>();
builder.Services.AddScoped<IOrderQuery, OrderQuery>();
builder.Services.AddScoped<ITaxRepository, TaxRepository>();
builder.Services.AddScoped<ITaxCommandService, TaxCommandService>();
builder.Services.AddScoped<ITaxQuery, TaxQuery>();
builder.Services.AddScoped<ITaxCalculationService, TaxCalculationService>();
builder.Services.AddScoped<IBillRepository, BillRepository>();
builder.Services.AddScoped<IUserQuery, UserQuery>();

// ── JWT Authentication ────────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (!string.IsNullOrEmpty(context.Request.Headers.Authorization))
            {
                return Task.CompletedTask;
            }

            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
                return Task.CompletedTask;
            }

            if (context.Request.Cookies.TryGetValue("SignalRContext", out var cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

            var tokenString = context.SecurityToken switch
            {
                System.IdentityModel.Tokens.Jwt.JwtSecurityToken jwt => jwt.RawData,
                Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jsonToken => jsonToken.EncodedToken,
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(tokenString) && cache.TryGetValue($"blacklist_{tokenString}", out _))
            {
                context.Fail("This token has been revoked.");
                return Task.CompletedTask;
            }

            var tableSessionIdClaim = context.Principal?.FindFirst("TableSessionId")?.Value;
            if (!string.IsNullOrEmpty(tableSessionIdClaim) &&
                cache.TryGetValue($"revoked_table_session_{tableSessionIdClaim}", out _))
            {
                context.Fail("This table session has ended.");
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            if (context.Exception is SecurityTokenExpiredException)
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("JWT Token expired for user connection.");
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(enRoleType.Admin.ToString()));

    options.AddPolicy("RequireStaff", policy =>
        policy.RequireRole(
            enRoleType.Admin.ToString(),
            enRoleType.Cashier.ToString()));
});

// ── Adding CORS policy ────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevelopmentPolicy", builder =>
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());

    options.AddPolicy("ProductionPolicy", builder =>
         builder.WithOrigins(
                "https://orderingsystem.tech",
                "https://web-five-tau-q7jp0rhb33.vercel.app"
               )
               .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
               //.WithHeaders("Authorization", "Content-Type", "x-requested-with", "x-signalr-user-agent", "x-device-session-id")
               .AllowAnyHeader() // Secure, since we have very few origins.
               .AllowCredentials());
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // Scoped HTTP Logging to Development only to prevent production I/O throttling
    app.UseHttpLogging();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseRouting();
    app.UseCors("DevelopmentPolicy");
}
else
{
    // Nginx handles HTTPS externally, so we only need Routing and CORS internally
    app.UseRouting();
    app.UseCors("ProductionPolicy");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers().RequireRateLimiting("DynamicRestaurantPolicy");
app.MapHub<TableSessionNotificationsHub>("/hubs/notifications/table-session");
app.MapHealthChecks("/api/health");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OrderingSystemDbContext>();
        var config = services.GetRequiredService<IConfiguration>();

        context.Database.Migrate();

        await DatabaseSeeder.SeedAdminUserAsync(context, config);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        throw;
    }
}
app.Run();