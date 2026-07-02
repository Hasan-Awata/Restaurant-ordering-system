using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class TableRepository : ITableRepository
    {
        private readonly OrderingSystemDbContext _context;

        public TableRepository(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task AddTableAsync(Table table)
        {
            _context.Tables.Add(table);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTableAsync(Table table)
        {
            _context.Tables.Update(table);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteTableAsync(Table table)
        {
            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(int tableNumber, int floor)
        {
            return await _context.Tables.AnyAsync(t => t.TableNumber == tableNumber && t.FloorNumber == floor);
        }

        public async Task<Table?> GetTableByIdAsync(int tableId)
        {
            return await _context.Tables.FirstOrDefaultAsync(t => t.TableId == tableId);
        }
    }
}