using Microsoft.EntityFrameworkCore;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class TableSessionRepository : ITableSessionRepository
    {
        private readonly OrderingSystemDbContext _context;

        public TableSessionRepository(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Table?> GetTableWithActiveSessionAsync(string qrCode)
        {
            // Fetches the domain entity for business rule validation in the Command service.
            // Includes the Session navigation property so you can check table.Session != null.
            return await _context.Tables
                    .Include(t => t.Sessions.Where(s => s.Status != enSessionStatus.Closed))
                    .ThenInclude(s => s.Devices)
                    .FirstOrDefaultAsync(t => t.QrCode == qrCode);
        }

        public async Task<TableSession?> GetActiveTableSessionWithOrdersAndDevicesAsync(Guid tableSessionId)
        {
            return await _context.TableSessions
            .Include(s => s.Orders)
            .Include(s => s.Devices)
            .FirstOrDefaultAsync(s => s.TableSessionId == tableSessionId && s.ClosedAt == null);
        }

        public async Task AddSessionAsync(TableSession session)
        {
            _context.TableSessions.Add(session);
            await _context.SaveChangesAsync();
        }

       public async Task<TableSession?> GetSessionByIdAsync(Guid tableSessionId)
        {
            return await _context.TableSessions
                .Include(s => s.Devices) 
                .FirstOrDefaultAsync(s => s.TableSessionId == tableSessionId);
        }

        public async Task UpdateSessionAsync(TableSession session)
        {
            _context.TableSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteSessionAsync(TableSession session)
        {
            _context.TableSessions.Remove(session);
            await _context.SaveChangesAsync();
        }
    }
}