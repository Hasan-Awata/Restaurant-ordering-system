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


        public async Task AddSessionAsync(TableSession session)
        {
            _context.TableSessions.Add(session);
            await _context.SaveChangesAsync();
        }
    }
}