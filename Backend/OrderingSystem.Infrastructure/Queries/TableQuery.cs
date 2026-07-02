using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Enums;
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
        
        public async Task<TableResponse?> GetTableByIdAsync(int tableId)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .Where(t => t.TableId == tableId)
                .Select(t => new TableResponse
                (
                    t.TableId,
                    t.TableNumber,
                    t.FloorNumber,
                    t.QrCode,
                    t.Status
                ))
                .FirstOrDefaultAsync();
            return table;
        }
        public async Task<TableResponse?> GetTableByNumberAsync(int tableNumber, int floorNumber)
        {
            var table = await _context.Tables
                .AsNoTracking()
                .Where(t => t.TableNumber == tableNumber && t.FloorNumber == floorNumber)
                .Select(t => new TableResponse
                (
                    t.TableId,
                    t.TableNumber,
                    t.FloorNumber,
                    t.QrCode,
                    t.Status
                ))
                .FirstOrDefaultAsync();
            return table;
        }
        public async Task<PagedResponse<TableResponse>> GetAllTablesByFloorAsync(int floorNumber)
        {
            throw new NotImplementedException();
        }
        public async Task<PagedResponse<TableResponse>> GetAllTablesAsync(PageDTO page)
        {
            throw new NotImplementedException();
        }
        public async Task<PagedResponse<TableResponse>> GetAllTablesByStatusAsync(PageDTO page, enTableStatus tableStatus)
        {
            throw new NotImplementedException();
        }
        public async Task<PagedResponse<TableResponse>> GetAllPendingActivationTablesAsync(PageDTO page)
        {
            throw new NotImplementedException();
        }
    }
}