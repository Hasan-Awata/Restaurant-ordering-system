using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.WebApi.Controllers.Base;
using System;
using System.Threading.Tasks;

namespace OrderingSystem.Controllers
{
    [ApiController]
    [Route("api/Admin")]
    [Authorize(Policy = "RequireStaff")]
    public class AdminController : BaseController
    {
        private readonly IOrderQuery _orderQuery;
        private readonly ITableQuery _tableQuery;

        public AdminController(IOrderQuery orderQuery, ITableQuery tableQuery)
        {
            _orderQuery = orderQuery;
            _tableQuery = tableQuery;
        }

        [HttpGet("top-three-today")]
        public async Task<IActionResult> GetTopThreeItemsToday()
        {
            var result = await _orderQuery.GetTopThreeItemsTodayAsync();
            return HandleResult(result);
        }

        [HttpGet("pending-count-orders")]
        public async Task<IActionResult> GetCountOfPendingOrders()
        {
            var result = await _orderQuery.GetCountOfPendingOrder();
            return HandleResult(result);
        }

        [HttpGet("total-count-orders")]
        public async Task<IActionResult> GetCountOfOrders()
        {
            var result = await _orderQuery.GetCountOfOrders();
            return HandleResult(result);
        }

        [HttpGet("total-count-table")]
        public async Task<IActionResult> GetCountOfTaple()
        {
            var result = await _tableQuery.GetCountTable();
            return HandleResult(result);
        }

        [HttpGet("historical-bills")] 
        public async Task<IActionResult> GetHistoricalBills(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] PageDTO page)
        {
        
            var result = await _orderQuery.GetHistoricalBillsAsync(startDate, endDate, page);
            return HandleResult(result);
        }
    }
}