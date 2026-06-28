using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Queries
{
    public class TableSessionQuery : ITableSessionQuery
    {
        private readonly OrderingSystemDbContext _context;

        public TableSessionQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        // ── Command-Side Hydration Query ─────────────────────────────────────────
        public async Task<Table?> GetTableWithActiveSessionAsync(string qrCode)
        {
            // Fetches the domain entity for business rule validation in the Command service.
            // Includes the Session navigation property so you can check table.Session != null.
            return await _context.Tables
                .Include(t => t.Session)
                .FirstOrDefaultAsync(t => t.QrCode == qrCode);
        }

        // ── Pure Read-Side Query (CQRS) ──────────────────────────────────────────
        public async Task<TableSessionResponse?> GetActiveSessionByTableAsync(int tableId)
        {
            // Bypasses entity tracking and maps directly from SQL to your DTO.
            // This eliminates the need for your SessionsMappers.cs on the read path entirely.
            return await _context.TableSessions
                    .AsNoTracking()
                    .Where(s => s.TableId == tableId && s.Status == enSessionStatus.Active)
                    .Select(s => new TableSessionResponse(
                        s.TableSessionId,
                        s.Table.TableNumber,
                        s.Status,
                        s.CreatedAt
                    ))
                    .FirstOrDefaultAsync();
        }
    }
}