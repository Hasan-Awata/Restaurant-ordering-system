using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Queries
{
    public class TableQuery : ITableQuery
    {
        private readonly OrderingSystemDbContext _context;

        public TableQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<string?> GetTableQrCodeAsync(int tableId)
        {
            return await _context.Tables
                .AsNoTracking()
                .Where(t => t.TableId == tableId)
                .Select(t => t.QrCode)
                .FirstOrDefaultAsync();
        }
    }
}