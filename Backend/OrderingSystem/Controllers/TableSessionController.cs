using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.DTOs;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/tables/sessions")]
    public class TableSessionsController : ControllerBase
    {
        private readonly ITableSessionCommand _commandService;
        private readonly ITableSessionQuery _queryService;

        // Explicit dependency routing
        public TableSessionsController(
            ITableSessionCommand commandService,
            ITableSessionQuery queryService)
        {
            _commandService = commandService;
            _queryService = queryService;
        }

        // 1. WRITE ENDPOINT (Command Path)
        [HttpPost("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivateSessionRequest request)
        {
            var response = await _commandService.ActivateAsync(request);
            return Ok(response);
        }

        // 2. READ ENDPOINT (Query Path)
        [HttpGet("active/{tableId}")]
        public async Task<IActionResult> GetActiveSession(int tableId)
        {
            var response = await _queryService.GetActiveSessionByTableAsync(tableId);
            if (response == null) return NotFound($"No active session found for table ID {tableId}.");

            return Ok(response);
        }
    }
}