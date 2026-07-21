using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableInterfaces
{
    public interface ITableQuery
    {
        public Task<string?> GetTableQrCodeAsync(int tableId);
        public Task<TableResponse?> GetTableByIdAsync(int tableId);
        public Task<TableResponse?> GetTableByNumberAsync(int tableNumber, int floorNumber);
        public Task<PagedResponse<TableResponse>> GetAllTablesByFloorAsync(PageDTO page, int floorNumber);
        public Task<PagedResponse<TableResponse>> GetAllTablesAsync(PageDTO page);
        public Task<PagedResponse<TableResponse>> GetAllTablesByStatusAsync(PageDTO page, enTableStatus tableStatus);
        public Task<PagedResponse<PendingTableResponse>> GetAllPendingActivationTablesAsync(PageDTO page);
        public Task<PagedResponse<PendingTableResponse>> GetAllBillingTablesAsync(PageDTO page);
    }
}
