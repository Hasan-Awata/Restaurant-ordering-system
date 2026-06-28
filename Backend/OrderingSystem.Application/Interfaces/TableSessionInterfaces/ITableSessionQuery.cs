using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ITableSessionQuery
    {
        public Task<Table?> GetTableWithActiveSessionAsync(string qrCode);

        public Task<TableSessionResponse?> GetActiveSessionByTableAsync(int tableId);
    }
}
