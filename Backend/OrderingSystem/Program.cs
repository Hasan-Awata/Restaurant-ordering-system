using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
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
using OrderingSystem.Application.Services;
using OrderingSystem.Infrastructure.Authentication;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Infrastructure.ExternalServices.Notifications;
using OrderingSystem.Infrastructure.Notifications;
using OrderingSystem.Infrastructure.Queries;
using OrderingSystem.Infrastructure.Repositories;
using OrderingSystem.Infrastructure.Seeding;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Caching ──────────────────────────────────────────────────────────────
builder.Services.AddMemoryCache();

// ── Controllers & JSON ───────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ── Rate Limiting ────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("Fixed", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 2;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.", cancellationToken: token);
    };
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
        b => b.MigrationsAssembly(typeof(OrderingSystemDbContext).Assembly.FullName)));

// ── Dependency Injections ──────────────────────────────────────────────────
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
        }
    };
});
builder.Services.AddAuthorization();

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
            builder.SetIsOriginAllowed(_ => true)
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials());
            //builder.WithOrigins(
            //    "http://127.0.0.1:5500",
            //    "http://localhost:3000",
            //    "http://localhost:8080"
            //   )
            //   .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            //   .WithHeaders("Authorization", "Content-Type", "x-requested-with", "x-signalr-user-agent")
            //   .AllowCredentials());
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();
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
app.MapControllers().RequireRateLimiting("Fixed");
app.MapHub<TableSessionNotificationsHub>("/hubs/notifications/table-session");

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