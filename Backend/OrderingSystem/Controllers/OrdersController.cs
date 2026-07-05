using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
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
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.Validation
                    ? BadRequest(result.ErrorMessage)
                    : StatusCode(500, result.ErrorMessage);
            }

            return CreatedAtAction(nameof(CreateOrder), new { id = result.Value!.OrderId }, result.Value);
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            var result = await _orderCommandService.ApproveOrderAsync(id);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Conflict) return Conflict(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(new { Message = "Order approved successfully." });
        }

        [Authorize(Roles = "Admin,Cashier")]
        [HttpDelete("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var result = await _orderCommandService.CancelOrderAsync(id);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return NoContent();
        }

        [HttpDelete("{id}/customer-cancel")]
        public async Task<IActionResult> CustomerCancelOrder(int id)
        {
            // Extract the secure session ID from the cookie
            if (!Request.Cookies.TryGetValue("DeviceSessionId", out var cookieValue) ||
                !Guid.TryParse(cookieValue, out var deviceSessionId))
            {
                return Unauthorized("Invalid or missing device session.");
            }

            var result = await _orderCommandService.CancelOrderByCustomerAsync(id, deviceSessionId);

            if (!result.IsSuccess)
            {
                return result.ErrorType switch
                {
                    enErrorType.NotFound => NotFound(result.ErrorMessage),
                    enErrorType.Unauthorized => Unauthorized(result.ErrorMessage),
                    enErrorType.Conflict => Conflict(result.ErrorMessage), // Returns 409 if order is already preparing
                    _ => StatusCode(500, result.ErrorMessage)
                };
            }

            return NoContent();
        }
    }
}