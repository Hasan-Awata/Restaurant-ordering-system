using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.DTOs
{
    public class OrderRecords
    {
        public record CreateOrderRequest(Guid TableSessionId, Guid DeviceSessionId, List<OrderItemRequest> Items);
        public record OrderItemRequest(int MenuItemId, int Quantity);

        public record OrderResponse(int OrderId, int TableNumber, decimal TotalAmount, enOrderStatus OrderStatus, DateTime CreatedAt, List<OrderItemResponse> Items);
        public record OrderItemResponse(int MenuItemId, string NameEn, string NameAr, int Quantity, decimal UnitPrice);
    }
}