using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.WebApi.Controllers.Base;

namespace OrderingSystem.Controllers
{
    [Route("api/Admin")]
    [ApiController]
    public class AdminController : BaseController
    {
        private readonly IOrderQuery _orderQuery;
        private readonly ITableQuery _tableQuery;

        public AdminController(IOrderQuery orderQuery, ITableQuery tableQuery)
        {
            _orderQuery = orderQuery;
            _tableQuery = tableQuery;
        }

         [Authorize(Roles = "Admin,Cashier")]
          [HttpGet("top-three-today")]
        public async Task<IActionResult> GetTopThreeItemsToday()
        {
            var result = await _orderQuery.GetTopThreeItemsTodayAsync();
            return HandleResult(result);
        }
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("pending-count-orders")]
        public async Task<IActionResult> GetCountOfPendingOrders()
        {
            var result = await _orderQuery.GetCountOfPendingOrder();
            return HandleResult(result);
        }

       [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("total-count-orders")]
        public async Task<IActionResult> GetCountOfOrders()
        {
            var result = await _orderQuery.GetCountOfOrders();
            return HandleResult(result);
        }
        //  [Authorize(Roles = "Admin,Cashier")]
        //[HttpGet("total-count-table")]
        //public async Task<IActionResult> GetCountOfOccupiedTables()
        //{
        //    var result = await _tableQuery.GetCountOfOccupiedTables();
        //    return HandleResult(result);
        //}

        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("total-count-table")]
        public async Task<IActionResult> GetCountOfTaple()
        {
            var result = await _tableQuery.GetCountTable();
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("historical-orders")]
        public async Task<IActionResult> GetHistoricalOrders(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] PageDTO page)
        {
            var result = await _orderQuery.GetOrdersByDateRangeAsync(startDate, endDate, page);
            return HandleResult(result);
        }

    }
}
