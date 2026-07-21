using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

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

          public async Task<PagedResponse<TableResponse>> GetAllTablesByFloorAsync(PageDTO page, int floorNumber)
        {
            var query = _context.Tables
                .AsNoTracking()
                .Where(t => t.FloorNumber == floorNumber);

            var totalRecords = await query.CountAsync();
            var tables = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(t => new TableResponse(t.TableId, t.TableNumber, t.FloorNumber, t.QrCode, t.Status))
                .ToListAsync();

            return new PagedResponse<TableResponse>(tables, totalRecords, page.PageNumber, page.PageSize);
        }

        
        public async Task<PagedResponse<TableResponse>> GetAllTablesAsync(PageDTO page)
        {
            var query = _context.Tables.AsNoTracking();
            var totalRecords = await query.CountAsync();

            var tables = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(t => new TableResponse(t.TableId, t.TableNumber, t.FloorNumber, t.QrCode, t.Status))
                .ToListAsync();

            return new PagedResponse<TableResponse>(tables, totalRecords, page.PageNumber, page.PageSize);
        }

    
        public async Task<PagedResponse<TableResponse>> GetAllTablesByStatusAsync(PageDTO page, enTableStatus tableStatus)
        {
            var query = _context.Tables
                .AsNoTracking()
                .Where(t => t.Status == tableStatus);

            var totalRecords = await query.CountAsync();

            var tables = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(t => new TableResponse(t.TableId, t.TableNumber, t.FloorNumber, t.QrCode, t.Status))
                .ToListAsync();

            return new PagedResponse<TableResponse>(tables, totalRecords, page.PageNumber, page.PageSize);
        }

    
        public async Task<PagedResponse<PendingTableResponse>> GetAllPendingActivationTablesAsync(PageDTO page)
        {
            var query = _context.Tables
                .AsNoTracking()
                .Where(t => t.Status == enTableStatus.Occupied && t.Sessions.Any(s => s.Status == enSessionStatus.PendingActivation && s.ClosedAt == null));

            var totalRecords = await query.CountAsync();

            var tables = await query
                    .Skip((page.PageNumber - 1) * page.PageSize)
                    .Take(page.PageSize)
                    .Select(t => new PendingTableResponse(
                        t.TableId,
                        t.TableNumber,
                        t.FloorNumber,
                        t.QrCode,
                        t.Status,
                        t.Sessions
                            .Where(s => s.Status == enSessionStatus.PendingActivation && s.ClosedAt == null)
                            .Select(s => s.TableSessionId)
                            .FirstOrDefault()
                    ))
                    .ToListAsync();

            return new PagedResponse<PendingTableResponse>(tables, totalRecords, page.PageNumber, page.PageSize);
        }

        public async Task<PagedResponse<PendingTableResponse>> GetAllBillingTablesAsync(PageDTO page)
        {
            var query = _context.Tables
                .AsNoTracking()
                .Where(t => t.Status == enTableStatus.Billing && t.Sessions.Any(s => s.ClosedAt == null));

            var totalRecords = await query.CountAsync();

            var tables = await query
                    .Skip((page.PageNumber - 1) * page.PageSize)
                    .Take(page.PageSize)
                    .Select(t => new PendingTableResponse(
                        t.TableId,
                        t.TableNumber,
                        t.FloorNumber,
                        t.QrCode,
                        t.Status,
                        t.Sessions
                            .Where(s => s.ClosedAt == null)
                            .Select(s => s.TableSessionId)
                            .FirstOrDefault()
                    ))
                    .ToListAsync();

            return new PagedResponse<PendingTableResponse>(tables, totalRecords, page.PageNumber, page.PageSize);
        }
    }
}