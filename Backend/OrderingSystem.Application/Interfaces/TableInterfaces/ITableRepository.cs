using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableInterfaces
{
    public interface ITableRepository
    {
        Task AddTableAsync(Table table);
        Task UpdateTableAsync(Table table);
        Task DeleteTableAsync(Table table);
        Task<bool> ExistsAsync(int tableNumber, int floor);

        // Reading methods (for business logic)
        public Task<Table?> GetByNumberAndFloorWithDeletedAsync(int tableNumber, int floor);
        public Task<Table?> GetTableByIdAsync(int tableId);
    }
}
