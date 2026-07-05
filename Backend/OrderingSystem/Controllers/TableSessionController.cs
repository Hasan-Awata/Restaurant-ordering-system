using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/tables/sessions")]
    public class TableSessionsController : ControllerBase
    {
        private readonly ISessionCommandService _sessionCommandService;
        private readonly ITableSessionQuery _sessionQueryService;

        // Explicit dependency routing
        public TableSessionsController(
            ISessionCommandService sessionCommandService,
            ITableSessionQuery sessionQueryService)
        {
            _sessionCommandService = sessionCommandService;
            _sessionQueryService = sessionQueryService;
        }

        // 1. WRITE ENDPOINT (Command Path)
        [HttpPost("qr")]
        public async Task<IActionResult> ProcessQrCode([FromBody] ProcessQrCodeRequest request)
        {
            // 1. Extract the secure cookie (if it exists)
            Guid? secureDeviceId = null;
            if (Request.Cookies.TryGetValue("DeviceSessionId", out var cookieValue) &&
                Guid.TryParse(cookieValue, out var parsedId))
            {
                secureDeviceId = parsedId;
            }

            // 2. Pass the clean DTO and the secure cookie value as separate parameters
            var response = await _sessionCommandService.ProcessTableQrCodeAsync(request.qrCode, secureDeviceId);

            // 3. If successful, set/refresh the secure cookie
            if (response.IsSuccess && response.Value?.DeviceSession != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddHours(4)
                };

                Response.Cookies.Append(
                    "DeviceSessionId",
                    response.Value.DeviceSession.DeviceSessionId.ToString(),
                    cookieOptions
                );
            }

            if (!response.IsSuccess) return BadRequest(response.ErrorMessage);
            return Ok(response.Value);
        }
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("activate")]
        public async Task<IActionResult> ActivateTableSession([FromBody] ActivateTableSessionRequest request)
        {
            var response = await _sessionCommandService.ActivateTableSessionAsync(request);

            if (!response.IsSuccess) return BadRequest(response.ErrorMessage);

            return Ok(response.Value);
        }

        [HttpPost("approve")]
        public async Task<IActionResult> ApproveGuest([FromBody] ApproveJoiningSessionRequest request)
        {
            // The service method for this was still intact in your dump
            var response = await _sessionCommandService.ApproveJoiningRequestAsync(request);

            if (!response.IsSuccess) return BadRequest(response.ErrorMessage);

            return Ok(response.Value);
        }

        // 2. READ ENDPOINT (Query Path)
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("active/{tableId}")]
        public async Task<IActionResult> GetActiveSession(int tableId)
        {
            var response = await _sessionQueryService.GetActiveSessionByTableAsync(tableId);
            if (response == null) return NotFound($"No active session found for table ID {tableId}.");

            return Ok(response);
        }
    }
}