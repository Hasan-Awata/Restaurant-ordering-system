using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ITableSessionQuery
    {
        public Task<TableSessionResponse?> GetActiveSessionByTableAsync(int tableId);
        public Task<BillSummaryResponse?> GetBillSummaryAsync(Guid tableSessionId);
    }
}
