using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ITableSessionRepository
    {
        public Task<Table?> GetTableWithActiveSessionAsync(string qrCode);
        public Task AddSessionAsync(TableSession session);
    }
}
