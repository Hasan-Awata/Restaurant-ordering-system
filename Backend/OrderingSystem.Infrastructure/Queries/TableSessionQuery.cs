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

        public async Task<TableSessionResponse?> GetActiveSessionByTableAsync(int tableId)
        {
            // Bypasses entity tracking and maps directly from SQL to your DTO.
            // This eliminates the need for your SessionsMappers.cs on the read path entirely.
            return await _context.TableSessions
                    .AsNoTracking()
                    .Where(s => s.TableId == tableId && s.ClosedAt == null)
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