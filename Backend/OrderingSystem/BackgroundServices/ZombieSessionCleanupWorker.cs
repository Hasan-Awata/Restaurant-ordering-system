using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrderingSystem.BackgroundServices
{
    public class ZombieSessionCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ZombieSessionCleanupWorker> _logger;

        public ZombieSessionCleanupWorker(IServiceProvider serviceProvider, ILogger<ZombieSessionCleanupWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Zombie Session Cleanup Worker is starting.");

            // Loop indefinitely until the application is shut down
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Background services are Singletons. To use Scoped services (like our CommandService and DbContext), 
                    // we must manually create a scope.
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var sessionCommandService = scope.ServiceProvider.GetRequiredService<ISessionCommandService>();

                        // Kill any session that has been pending for more than 15 minutes
                        await sessionCommandService.CleanupZombieSessionsAsync(TimeSpan.FromMinutes(15));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up zombie sessions.");
                }

                // Wait 5 minutes before checking again to avoid stressing the database
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}