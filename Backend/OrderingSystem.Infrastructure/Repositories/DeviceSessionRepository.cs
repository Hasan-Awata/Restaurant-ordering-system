using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class DeviceSessionRepository : IDeviceSessionRepository
    {
        private readonly OrderingSystemDbContext _context;

        public DeviceSessionRepository(OrderingSystemDbContext context)
        {
            _context = context;
        }

        // Reading Path
        public async Task<DeviceSession?> GetDeviceSessionByIdAsync(Guid deviceSessionId)
        {
            return await _context.SessionDevices
                    .Include(ds => ds.TableSession) 
                    .FirstOrDefaultAsync(ds => ds.DeviceSessionId == deviceSessionId);
        }

        public async Task AddSessionAsync(DeviceSession session)
        {
            _context.SessionDevices.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateDeviceSessionAsync(DeviceSession session)
        {
            _context.SessionDevices.Update(session);
            await _context.SaveChangesAsync();
        }
    }
}