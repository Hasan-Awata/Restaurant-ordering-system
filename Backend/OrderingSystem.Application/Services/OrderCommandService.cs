using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.Services
{
    public class OrderCommandService : IOrderCommandService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMenueItemRepository _menuItemRepository;
        private readonly IRealTimeNotifier _notifier;

        public OrderCommandService(
            IOrderRepository orderRepository,
            IMenueItemRepository menuItemRepository,
            IRealTimeNotifier notifier)
        {
            _orderRepository = orderRepository;
            _menuItemRepository = menuItemRepository;
            _notifier = notifier;
        }

        public async Task<Result<OrderRecords.OrderResponse>> AddOrderAsync(OrderRecords.CreateOrderRequest request)
        {
            if (!request.Items.Any())
                return Result<OrderRecords.OrderResponse>.Failure("Order must contain at least one item.", enErrorType.Validation);

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();

            foreach (var itemReq in request.Items)
            {
                var menuItem = await _menuItemRepository.GetMenuItemByIdAsync(itemReq.MenuItemId);
                if (menuItem == null || !menuItem.IsAvailable)
                    return Result<OrderRecords.OrderResponse>.Failure($"Menu item {itemReq.MenuItemId} is unavailable.", enErrorType.Validation);

                totalAmount += menuItem.Price * itemReq.Quantity;

                orderItems.Add(new OrderItem
                {
                    MenuItemId = menuItem.MenuItemId,
                    Quantity = itemReq.Quantity,
                    UnitPrice = menuItem.Price, // Securely sourced from DB
                    Notes = itemReq.Notes
                });
            }

            var order = new Order
            {
                TableSessionId = request.TableSessionId,
                DeviceSessionId = request.DeviceSessionId,
                TotalAmount = totalAmount,
                OrderStatus = enOrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            await _orderRepository.AddOrderAsync(order);

            // Notify Cashiers
            await _notifier.NotifyCashierOfNewOrderAsync(order.OrderId, order.TableSessionId);

            // Construct Response
            var responseItems = order.OrderItems.Select(oi => new OrderRecords.OrderItemResponse(
                oi.MenuItemId, oi.MenuItem.NameEn, oi.MenuItem.NameAr, oi.Quantity, oi.UnitPrice, oi.Notes)).ToList();

            var response = new OrderRecords.OrderResponse(order.OrderId, request.TableNumber, order.TotalAmount, order.OrderStatus, order.CreatedAt, responseItems);
            return Result<OrderRecords.OrderResponse>.Success(response);
        }

        public async Task<Result<bool>> ApproveOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return Result<bool>.Failure("Order not found.", enErrorType.NotFound);

            if (order.OrderStatus != enOrderStatus.Pending)
                return Result<bool>.Failure("Only pending orders can be approved.", enErrorType.Conflict);

            order.OrderStatus = enOrderStatus.Preparing;
            await _orderRepository.UpdateOrderAsync(order);

            // Notify Customer
            await _notifier.NotifyCustomerOfOrderStatusAsync(order.DeviceSessionId, order.OrderId, order.OrderStatus);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CancelOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return Result<bool>.Failure("Order not found.", enErrorType.NotFound);

            // Notify Customer before deletion so we still have the DeviceSessionId
            await _notifier.NotifyCustomerOfOrderStatusAsync(order.DeviceSessionId, order.OrderId, enOrderStatus.Cancelled);

            // Per requirement: Order is immediately deleted from the database
            await _orderRepository.DeleteOrderAsync(order);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CancelOrderByCustomerAsync(int orderId, Guid deviceSessionId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return Result<bool>.Failure("Order not found.", enErrorType.NotFound);

            // 1. Security Guard: Does this order belong to the device trying to cancel it?
            if (order.DeviceSessionId != deviceSessionId)
                return Result<bool>.Failure("You are not authorized to cancel this order.", enErrorType.Unauthorized);

            // 2. Business Rule Guard: Has the kitchen already started?
            if (order.OrderStatus != enOrderStatus.Pending)
                return Result<bool>.Failure("Your order is already being prepared. Please speak to the cashier to cancel.", enErrorType.Conflict);

            // 3. Execution
            await _orderRepository.DeleteOrderAsync(order);

            // 4. Notification
            await _notifier.NotifyCashiersOfCustomerCancellationAsync(order.OrderId, order.TableSessionId);

            return Result<bool>.Success(true);
        }
    }
}