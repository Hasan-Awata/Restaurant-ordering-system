// File: OrderingSystem.Infrastructure/Services/TableSessionRepository.cs

using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class TableSessionRepository : ITableSessionRepository, ITableSessionQuery
    {
        private readonly OrderingSystemDbContext _context;

        public TableSessionRepository(OrderingSystemDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // WRITE PATH (ITableSessionRepository)
        // ==========================================
        public async Task<Table?> GetTableWithActiveSessionAsync(int tableId)
        {
            // Tracks entity state for changes
            return await _context.Tables.FirstOrDefaultAsync(t => t.TableId == tableId);
        }

        public async Task SaveSessionAsync(TableSession session)
        {
            _context.TableSessions.Add(session);
            await _context.SaveChangesAsync();
        }

        // ==========================================
        // READ PATH (ITableSessionQueryService)
        // ==========================================
        public async Task<SessionResponse?> GetActiveSessionByTableAsync(int tableId)
        {
            var entity = await _context.TableSessions
                .Include(s => s.Table)
                .AsNoTracking() // Read optimization
                .FirstOrDefaultAsync(s => s.TableId == tableId && s.Status == enSessionStatus.Active);

            return entity?.ToResponse();
        }
    }
}