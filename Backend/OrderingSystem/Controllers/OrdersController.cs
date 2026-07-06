using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Domain.Enums;
using OrderingSystem.WebApi.Controllers.Base;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : BaseController
    {
        private readonly IOrderCommandService _orderCommandService;

        public OrdersController(IOrderCommandService orderCommandService)
        {
            _orderCommandService = orderCommandService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRecords.CreateOrderRequest request)
        {
            var result = await _orderCommandService.AddOrderAsync(request);

            return HandleCreatedResult(
                result,
                nameof(CreateOrder),
                new { id = result.Value?.OrderId }
            );
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            var result = await _orderCommandService.ApproveOrderAsync(id);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpDelete("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var result = await _orderCommandService.CancelOrderAsync(id);
            return HandleResult(result);
        }

        [HttpDelete("{id}/customer-cancel")]
        public async Task<IActionResult> CustomerCancelOrder(int id)
        {
            if (!CurrentDeviceSessionId.HasValue)
            {
                return Unauthorized(new { error = "Invalid or missing device session." });
            }

            var result = await _orderCommandService.CancelOrderByCustomerAsync(id, CurrentDeviceSessionId.Value);
            return HandleResult(result);
        }
    }
}