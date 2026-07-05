using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Interfaces.OrdersInterfaces
{
    public interface IOrderRepository
    {
        Task AddOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(Order order);
    }
}