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

        public async Task AddSessionAsync(TableSession session)
        {
            _context.TableSessions.Add(session);
            await _context.SaveChangesAsync();
        }
    }
}