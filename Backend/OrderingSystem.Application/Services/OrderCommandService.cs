using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Application.Interfaces.Notifications;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.SessionsInterfaces;
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
        private readonly IDeviceSessionRepository _deviceSessionRepository;

        public OrderCommandService(
            IOrderRepository orderRepository,
            IMenueItemRepository menuItemRepository,
            IRealTimeNotifier notifier,
            IDeviceSessionRepository deviceSessionRepository)
        {
            _orderRepository = orderRepository;
            _menuItemRepository = menuItemRepository;
            _notifier = notifier;
            _deviceSessionRepository = deviceSessionRepository;
        }

        public async Task<Result<OrderRecords.OrderResponse>> AddOrderAsync(OrderRecords.CreateOrderRequest request, Guid deviceSessionId)
        {
            if (request == null)
                return Result<OrderRecords.OrderResponse>.Failure("Request cannot be null.", enErrorType.Validation);

            if (request.TableNumber <= 0)
                return Result<OrderRecords.OrderResponse>.Failure("Table number must be greater than zero.", enErrorType.Validation);

            if (!request.Items.Any())
                return Result<OrderRecords.OrderResponse>.Failure("Order must contain at least one item.", enErrorType.Validation);

            var deviceSession = await _deviceSessionRepository.GetDeviceSessionByIdAsync(deviceSessionId);

            if (deviceSession == null || (!deviceSession.IsApproved && deviceSession.Role != enDeviceRole.Host))
                return Result<OrderRecords.OrderResponse>.Failure("You must be approved by the table host before ordering.", enErrorType.Unauthorized);

            if (deviceSession.TableSession.Table.Status == enTableStatus.Billing)
                return Result<OrderRecords.OrderResponse>.Failure("Orders cannot be placed while the table is processing the bill.", enErrorType.Conflict);

            if (deviceSession.TableSessionId != request.TableSessionId)
                return Result<OrderRecords.OrderResponse>.Failure("The device session does not belong to the requested table session.", enErrorType.Unauthorized);

            // 1. Fetch all items in a single query before the loop
            var requestedItemIds = request.Items.Select(i => i.MenuItemId).Distinct();
            var fetchedItems = await _menuItemRepository.GetMenuItemsByIdsAsync(requestedItemIds);
            var menuItemsDict = fetchedItems.ToDictionary(m => m.MenuItemId);

            decimal totalAmount = 0;
            var orderItems = new List<OrderItem>();
            var responseItems = new List<OrderRecords.OrderItemResponse>();

            foreach (var itemReq in request.Items)
            {
                // 2. Read from the dictionary instead of the database
                if (!menuItemsDict.TryGetValue(itemReq.MenuItemId, out var menuItem) || !menuItem.IsAvailable)
                    return Result<OrderRecords.OrderResponse>.Failure($"Menu item {itemReq.MenuItemId} is unavailable.", enErrorType.Validation);

                totalAmount += menuItem.Price * itemReq.Quantity;

                orderItems.Add(new OrderItem
                {
                    MenuItemId = menuItem.MenuItemId,
                    Quantity = itemReq.Quantity,
                    UnitPrice = menuItem.Price,
                    Notes = itemReq.Notes
                });

                responseItems.Add(new OrderRecords.OrderItemResponse(
                    menuItem.MenuItemId, menuItem.NameEn, menuItem.NameAr, itemReq.Quantity, menuItem.Price, itemReq.Notes));
            }

            var order = new Order
            {
                TableSessionId = request.TableSessionId,
                DeviceSessionId = deviceSessionId,
                TotalAmount = totalAmount,
                OrderStatus = enOrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            await _orderRepository.AddOrderAsync(order);
            await _notifier.NotifyCashierOfNewOrderAsync(order.OrderId, order.TableSessionId);

            // --- 3. USE THE PRE-MAPPED LIST ---
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

            // Notify Cashier
            await _notifier.NotifyCashiersOfOrderSyncAsync(order.OrderId, order.OrderStatus);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CancelOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return Result<bool>.Failure("Order not found.", enErrorType.NotFound);

            await _notifier.NotifyCustomerOfOrderRejectedAsync(order.DeviceSessionId, order.OrderId);

            // Soft delete for auditing purposes
            await _orderRepository.DeleteOrderAsync(order);

            return Result<bool>.Success(true);
        }

        public async Task<Result<bool>> CancelOrderByCustomerAsync(int orderId, Guid deviceSessionId)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order == null)
                return Result<bool>.Failure("Order not found.", enErrorType.NotFound);

            // 1. Security Guard
            if (order.DeviceSessionId != deviceSessionId && order.Device.Role != enDeviceRole.Host)
                return Result<bool>.Failure("You are not authorized to cancel this order.", enErrorType.Unauthorized);

            // 2. Business Rule Guard
            if (order.OrderStatus != enOrderStatus.Pending)
                return Result<bool>.Failure("Your order is already being prepared. Please speak to the cashier to cancel.", enErrorType.Conflict);

            // 3. Execution 
            order.OrderStatus = enOrderStatus.Cancelled;
            await _orderRepository.UpdateOrderAsync(order);

            // 4. Notification
            await _notifier.NotifyCashiersOfCustomerCancellationAsync(order.OrderId, order.TableSessionId);
            await _notifier.NotifyCashiersOfOrderSyncAsync(order.OrderId, order.OrderStatus);


            return Result<bool>.Success(true);
        }
    }
}