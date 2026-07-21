using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged; // تمت إضافة هذا السطر من أجل PageDTO
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Enums;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/tables")]
    public class TablesController : ControllerBase
    {
        private readonly ITableCommandService _tableCommandService;
        private readonly ITableQuery _tableQueryService;

        public TablesController(ITableCommandService tableCommandService, ITableQuery tableQueryService)
        {
            _tableCommandService = tableCommandService;
            _tableQueryService = tableQueryService;
        }

        // ==========================================
        // الكود القديم الخاص بك (لم يتم تغييره)
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> AddTable([FromBody] AddTableRequest request)
        {
            var result = await _tableCommandService.AddTableAsync(request);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.Validation ? BadRequest(result.ErrorMessage) : StatusCode(500, result.ErrorMessage);
            }

            return CreatedAtAction(nameof(GetTableById), new { tableId = result.Value!.TableId }, result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateTable([FromBody] UpdateTableRequest request)
        {
            var result = await _tableCommandService.UpdateTableAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Validation) return BadRequest(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }
        
        [HttpDelete("{tableId}")]
        public async Task<IActionResult> DeleteTable(int tableId)
        {
            var result = await _tableCommandService.DeleteTableAsync(tableId);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Validation) return BadRequest(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return NoContent();
        }

        [HttpGet("{tableId}/qrcode")]
        public async Task<IActionResult> GetQrCode(int tableId)
        {
            var qrCode = await _tableQueryService.GetTableQrCodeAsync(tableId);
            if (qrCode == null) return NotFound($"Table ID {tableId} not found.");

            return Ok(new { QrCode = qrCode });
        }

        

        [HttpGet("{tableId}")]
        public async Task<IActionResult> GetTableById(int tableId)
        {
            var result = await _tableQueryService.GetTableByIdAsync(tableId);
            if (result == null) return NotFound($"Table ID {tableId} not found.");

            return Ok(result);
        }

        [HttpGet("floor/{floorNumber}/number/{tableNumber}")]
        public async Task<IActionResult> GetTableByNumber(int tableNumber, int floorNumber)
        {
            var result = await _tableQueryService.GetTableByNumberAsync(tableNumber, floorNumber);
            if (result == null) return NotFound($"Table Number {tableNumber} on Floor {floorNumber} not found.");

            return Ok(result);
        }

        [HttpGet("floor/{floorNumber}")]
        public async Task<IActionResult> GetAllTablesByFloor([FromQuery] PageDTO page, int floorNumber)
        {
            var result = await _tableQueryService.GetAllTablesByFloorAsync(page, floorNumber);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTables([FromQuery] PageDTO page)
        {
            var result = await _tableQueryService.GetAllTablesAsync(page);
            return Ok(result);
        }

        [HttpGet("status/{tableStatus}")]
        public async Task<IActionResult> GetAllTablesByStatus([FromQuery] PageDTO page, enTableStatus tableStatus)
        {
            var result = await _tableQueryService.GetAllTablesByStatusAsync(page, tableStatus);
            return Ok(result);
        }

        [HttpGet("pending-activation")]
        public async Task<IActionResult> GetAllPendingActivationTables([FromQuery] PageDTO page)
        {
            var result = await _tableQueryService.GetAllPendingActivationTablesAsync(page);
            return Ok(result);
        }
    }
}