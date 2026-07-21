using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ITableSessionRepository
    {
        public Task<Table?> GetTableWithActiveSessionAsync(string qrCode);
        public Task<TableSession?> GetActiveTableSessionWithOrdersAndDevicesAsync(Guid tableSessionId);
        public Task DeleteSessionAsync(TableSession session);
        public Task AddSessionAsync(TableSession session);
        public Task<TableSession?> GetSessionByIdAsync(Guid tableSessionId);
        public Task UpdateSessionAsync(TableSession session);
    }
}
