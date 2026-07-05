using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Seeding
{
    public static class DatabaseSeeder
    {
        public static async Task SeedAdminUserAsync(OrderingSystemDbContext context, IConfiguration config)
        {
            var defaultPassword = config["AdminSettings:DefaultPassword"]
                    ?? throw new InvalidOperationException("AdminSettings:DefaultPassword is missing from configuration.");

            var defaultFullName = config["AdminSettings:FullName"] ?? "MasterAdmin";

            if (!await context.Users.AnyAsync(u => u.Role == enRoleType.Admin))
            {
                var initialAdmin = new User
                {
                    FullName = defaultFullName,
                    PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(defaultPassword),
                    Role = enRoleType.Admin
                };

                context.Users.Add(initialAdmin);
                await context.SaveChangesAsync();
            }
        }
    }
}