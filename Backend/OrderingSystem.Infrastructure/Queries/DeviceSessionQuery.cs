using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Queries
{
    public class DeviceSessionQuery : IDeviceSessionQuery
    {
        private readonly OrderingSystemDbContext _context;

        public DeviceSessionQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<DeviceSession?> GetDeviceSessionByIdAsync(Guid deviceSessionId)
        {
            return await _context.SessionDevices
                .Include(ds => ds.TableSession) // Required because your command service uses ds.TableSession for mapping responses
                .FirstOrDefaultAsync(ds => ds.DeviceSessionId == deviceSessionId);
        }
    }
}