using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ITableSessionRepository
    {
        public Task<Table?> GetTableWithActiveSessionAsync(int tableId);
        public Task SaveSessionAsync(TableSession session);
    }
}
