using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.Data;
using System;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly OrderingSystemDbContext _context;

        public UnitOfWork(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task ExecuteTransactionAsync(Func<Task> action)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await action();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw; // Let the GlobalExceptionHandler catch it
                }
            });
        }
    }
}