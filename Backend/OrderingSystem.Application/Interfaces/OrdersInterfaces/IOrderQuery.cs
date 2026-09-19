using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using static OrderingSystem.Application.DTOs.OrderRecords;

namespace OrderingSystem.Application.Interfaces.OrdersInterfaces
{
    public interface IOrderQuery
    {
        public Task<Result<List<OrderRecords.OrderItemResponse>>> GetTopThreeItemsTodayAsync();
     
        public Task<Result<int>> GetCountOfOrders();
        public Task<Result<int>> GetCountOfPendingOrder();
        public Task<Result<PagedResponse<OrderRecords.OrderResponse>>> GetPendingOrdersAsync(PageDTO page);
        public Task<Result<PagedResponse<OrderRecords.OrderResponse>>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, PageDTO page);
      public   Task<Result<PagedResponse<HistoricalBillResponse>>> GetHistoricalBillsAsync(
            DateTime startDate,
            DateTime endDate,
            PageDTO page);
    }
}
