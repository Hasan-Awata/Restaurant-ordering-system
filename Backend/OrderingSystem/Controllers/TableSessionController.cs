using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.DTOs;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/tables/sessions")]
    public class TableSessionsController : ControllerBase
    {
        private readonly ISessionCommand _sessionCommandService;
        private readonly ITableSessionQuery _sessionQueryService;

        // Explicit dependency routing
        public TableSessionsController(
            ISessionCommand sessionCommandService,
            ITableSessionQuery sessionQueryService)
        {
            _sessionCommandService = sessionCommandService;
            _sessionQueryService = sessionQueryService;
        }

        // 1. WRITE ENDPOINT (Command Path)
        [HttpPost("qr")]
        public async Task<IActionResult> ProcessQrCode([FromBody] ProcessQrCodeRequest request)
        {
            var response = await _sessionCommandService.ProcessTableQrCodeAsync(request);
            return Ok(response);
        }

        // 2. READ ENDPOINT (Query Path)
        [HttpGet("active/{tableId}")]
        public async Task<IActionResult> GetActiveSession(int tableId)
        {
            var response = await _sessionQueryService.GetActiveSessionByTableAsync(tableId);
            if (response == null) return NotFound($"No active session found for table ID {tableId}.");

            return Ok(response);
        }
    }
}