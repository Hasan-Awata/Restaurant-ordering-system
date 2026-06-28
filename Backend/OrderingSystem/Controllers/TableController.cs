using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Enums;

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

        [HttpPost]
        public async Task<IActionResult> AddTable([FromBody] AddTableRequest request)
        {
            var result = await _tableCommandService.AddTableAsync(request);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.Validation ? BadRequest(result.ErrorMessage) : StatusCode(500, result.ErrorMessage);
            }
            return CreatedAtAction(nameof(GetQrCode), new { tableId = result.Value!.TableId }, result.Value);
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
    }
}