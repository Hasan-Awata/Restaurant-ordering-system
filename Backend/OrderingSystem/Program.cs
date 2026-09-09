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
using OrderingSystem.Application.Interfaces.Category;
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
    // Scrub the JWT token from the logs
    logging.RequestHeaders.Add("Authorization");
    logging.MediaTypeOptions.AddText("application/json");
    // This is the critical line:
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
    // Return a standard 429 status code when limits are hit
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("DynamicRestaurantPolicy", httpContext =>
    {
        // 1. Check if the user is already authenticated via device session
        var hasDeviceSession = httpContext.Request.Cookies.TryGetValue("DeviceSessionId", out var sessionId);

        if (hasDeviceSession && !string.IsNullOrWhiteSpace(sessionId))
        {
            // Authenticated Device: Standard limit per individual device
            return RateLimitPartition.GetFixedWindowLimiter(sessionId, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 150, 
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10 // Increased queue to absorb brief network reconnects
                });
        }

        // 2. Anonymous Request: IP-based partition (Shared Restaurant Wi-Fi)
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.TraceIdentifier;

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 1000, // Massive threshold to account for NAT/Shared Wi-Fi
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 50 // Large queue to prevent dropping legitimate new connections
            });
    });

    options.AddPolicy("LoginPolicy", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.TraceIdentifier;

        return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5, // 5 tries only
                Window = TimeSpan.FromMinutes(1), // every minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0 // reject the request immediately if the limit is exceeded
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
builder.Services.AddDbContext<OrderingSystemDbContext>(options =>
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
// Register the Global Exception Handler and standard Problem Details
builder.Services.AddExceptionHandler<OrderingSystem.WebApi.Middleware.GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSignalR();
builder.Services.AddScoped<IRealTimeNotifier, SignalRNotifier>();

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
        IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(secretKey))
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            // If the request is for our SignalR hub and contains a token in the query
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                // Tell the middleware to use this token for authentication
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },

        OnTokenValidated = context =>
        {
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var tokenString = context.SecurityToken is JwtSecurityToken jwt ? jwt.RawData : string.Empty;

            // If the token is found in the cache, it's revoked. Reject the request.
            if (!string.IsNullOrEmpty(tokenString) && cache.TryGetValue($"blacklist_{tokenString}", out _))
            {
                context.Fail("This token has been revoked.");
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Strict access for Managers/Owners
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole(enRoleType.Admin.ToString()));

    // Operational access for Front-of-House
    options.AddPolicy("RequireStaff", policy =>
        policy.RequireRole(
            enRoleType.Admin.ToString(),
            enRoleType.Cashier.ToString())); 
});

// ── Adding CORS policy ────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    // 1. Wide-open policy for local development only
    options.AddPolicy("DevelopmentPolicy", builder =>
        builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());

    // 2. Iron-clad policy for Production
    options.AddPolicy("ProductionPolicy", builder =>
         builder.WithOrigins(
                "http://127.0.0.1:5500",
                "http://localhost:3000",
                "http://localhost:8080",
                "https://courageous-pika-0f4f00.netlify.app",
                "https://web-five-tau-q7jp0rhb33.vercel.app"
               )
               .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
               .WithHeaders("Authorization", "Content-Type", "x-requested-with", "x-signalr-user-agent", "x-device-session-id")
               .AllowCredentials());
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseHttpLogging(); 
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseRouting();
    // Use the wide-open policy locally
    app.UseCors("DevelopmentPolicy");
}
else
{
    // Enforce HTTPS routing in production
    app.UseHttpsRedirection();
    app.UseRouting();
    // Use the locked-down policy in production
    app.UseCors("ProductionPolicy");
}

app.UseAuthentication();
app.UseAuthorization();
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