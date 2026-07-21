using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;

namespace OrderingSystem.Application.Interfaces.OrdersInterfaces
{
    public interface IOrderCommandService
    {
        Task<Result<OrderRecords.OrderResponse>> AddOrderAsync(OrderRecords.CreateOrderRequest request);
        Task<Result<bool>> ApproveOrderAsync(int orderId);
        Task<Result<bool>> CancelOrderAsync(int orderId);
        Task<Result<bool>> CancelOrderByCustomerAsync(int orderId, Guid deviceSessionId);
    }
}