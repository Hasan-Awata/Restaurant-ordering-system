using OrderingSystem.Domain.Entities;

public interface IOrderRepository
{
    Task AddOrderAsync(Order order);
    Task<Order?> GetOrderByIdAsync(int orderId);
    Task UpdateOrderAsync(Order order);
    Task UpdateOrdersAsync(IEnumerable<Order> orders);
    Task DeleteOrderAsync(Order order); // Soft delete
    Task HardDeleteOrderAsync(Order order); // Hard delete
    Task HardDeleteOrdersAsync(IEnumerable<Order> orders);
}