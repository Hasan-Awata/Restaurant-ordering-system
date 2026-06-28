using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Services;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Infrastructure.ExternalServices.Notifications;
using OrderingSystem.Infrastructure.Notifications;
using OrderingSystem.Infrastructure.Queries;
using OrderingSystem.Infrastructure.Repositories;
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
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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
});

builder.Services.AddAuthorization();

// ── Allowing all requests for testing ────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<TableSessionNotificationsHub>("/hubs/notifications/table-session");

app.UseHttpsRedirection();
app.UseCors("AllowAll"); // Delete on actual deployment
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("Fixed");

app.Run();