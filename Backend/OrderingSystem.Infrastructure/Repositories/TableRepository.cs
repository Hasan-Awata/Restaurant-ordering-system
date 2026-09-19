using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
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

        public async Task UpdateTableAsync(Table table, uint? originalVersion = null)
        {
            _context.Tables.Update(table);

            // Only override the tracker if the frontend sent a specific version to check against
            if (originalVersion.HasValue)
            {
                _context.Entry(table).Property(e => e.Version).OriginalValue = originalVersion.Value;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteTableAsync(Table table)
        {
            table.IsDeleted = true;
            table.Status = enTableStatus.Available; 

            _context.Tables.Update(table);
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

        public async Task<Table?> GetByNumberAndFloorWithDeletedAsync(int tableNumber, int floor)
        {
            return await _context.Tables
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.TableNumber == tableNumber && t.FloorNumber == floor);
        }
    }
}