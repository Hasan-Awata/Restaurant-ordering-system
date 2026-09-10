using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.WebApi.Controllers.Base;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/tables/sessions")]
    public class TableSessionsController : BaseController
    {
        private readonly ISessionCommandService _sessionCommandService;
        private readonly ITableSessionQuery _sessionQueryService;

        public TableSessionsController(
            ISessionCommandService sessionCommandService,
            ITableSessionQuery sessionQueryService)
        {
            _sessionCommandService = sessionCommandService;
            _sessionQueryService = sessionQueryService;
        }

        // 1. WRITE ENDPOINT (Command Path)

        // ── CUSTOMER PATH: Scan the QR code ─────────────────────────────────────
        [HttpPost("qr")]
        [DisableRateLimiting]
        public async Task<IActionResult> ProcessQrCode([FromBody] ProcessQrCodeRequest request)
        {
            var result = await _sessionCommandService.ProcessTableQrCodeAsync(request.qrCode, CurrentDeviceSessionId);

            if (result.IsSuccess && result.Value?.DeviceSession != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddHours(4)
                };

                // 1. Maintain original behavior for backward compatibility
                Response.Cookies.Append(
                    "DeviceSessionId",
                    result.Value.DeviceSession.DeviceSessionId.ToString(),
                    cookieOptions
                );

                // 2. Generate and append the stateless context token
                var jwtProvider = HttpContext.RequestServices.GetRequiredService<IJwtProvider>();
                var signalRToken = jwtProvider.GenerateCustomerSignalRToken(
                    result.Value.DeviceSession.DeviceSessionId,
                    result.Value.TableSession.TableSessionId
                );

                Response.Cookies.Append("SignalRContext", signalRToken, cookieOptions);

                // 3. For Flutter applications, return the token in the response body as well
                return Ok(new
                {
                    tableSession = result.Value.TableSession,
                    deviceSession = result.Value.DeviceSession,
                    accessToken = signalRToken
                });
            }

            return HandleResult(result);
        }

        // ── CASHIER PATH: Approve the activation request ────────────────────────
        [Authorize(Policy = "RequireStaff")]
        [HttpPost("activate")]
        public async Task<IActionResult> ActivateTableSession([FromBody] ActivateTableSessionRequest request)
        {
            var result = await _sessionCommandService.ActivateTableSessionAsync(request);
            return HandleResult(result);
        }

        // ── CUSTOMER PATH: Approve the guests ───────────────────────────────────
        [HttpPost("approve")]
        public async Task<IActionResult> ApproveGuest([FromBody] ApproveJoiningSessionRequest request)
        {
            if (!CurrentDeviceSessionId.HasValue)
                return Unauthorized(new { error = "Invalid or missing device session." });

            var result = await _sessionCommandService.ApproveJoiningRequestAsync(request, CurrentDeviceSessionId.Value);
            return HandleResult(result);
        }

        // ── CUSTOMER PATH: Request the bill ─────────────────────────────────────
        [HttpPost("request-bill")]
        public async Task<IActionResult> RequestBill([FromBody] RequestBillRequest request)
        {
            if (!CurrentDeviceSessionId.HasValue)
            {
                return Unauthorized(new { error = "Invalid or missing device session." });
            }

            var result = await _sessionCommandService.RequestBillAsync(request.tableSessionId, CurrentDeviceSessionId.Value);
            return HandleResult(result);
        }

        // ── CASHIER PATH: Close session after payment ───────────────────────────
        [Authorize(Policy = "RequireStaff")]
        [HttpPost("end")]
        public async Task<IActionResult> EndTableSession([FromBody] ActivateTableSessionRequest request)
        {
            // Reusing ActivateTableSessionRequest since it only contains the TableSessionId
            var result = await _sessionCommandService.EndTableSessionAsync(request.tableSessionId);
            return HandleResult(result);
        }

        [HttpGet("{tableSessionId}/bill")]
        public async Task<IActionResult> GetBillSummary(Guid tableSessionId)
        {
            var result = await _sessionQueryService.GetBillSummaryAsync(tableSessionId);

            if (result == null)
                return NotFound(new { error = $"No bill found for session ID {tableSessionId}." });

            return Ok(result);
        }

        [Authorize(Policy = "RequireStaff")]
        [HttpPost("dismiss")]
        public async Task<IActionResult> DismissTableSession([FromBody] ActivateTableSessionRequest request)
        {
            var result = await _sessionCommandService.DismissTableSessionAsync(request.tableSessionId);
            return HandleResult(result);
        }

        [Authorize(Policy = "RequireStaff")]
        [HttpPost("dismiss-bill")]
        public async Task<IActionResult> DismissBill([FromBody] ActivateTableSessionRequest request)
        {
            var result = await _sessionCommandService.DismissBillAsync(request.tableSessionId);
            return HandleResult(result);
        }

        // 2. READ ENDPOINT (Query Path)
        [Authorize(Policy = "RequireStaff")]
        [HttpGet("active/{tableId}")]
        public async Task<IActionResult> GetActiveSession(int tableId)
        {
            // Keeping standard Ok/NotFound since the Query returns a raw response, not Result<T>
            var response = await _sessionQueryService.GetActiveSessionByTableAsync(tableId);
            if (response == null) return NotFound(new { error = $"No active session found for table ID {tableId}." });

            return Ok(response);
        }

        [HttpGet("{tableSessionId}/status")]
        public async Task<IActionResult> GetSessionPollingStatus(Guid tableSessionId)
        {
            if (!CurrentDeviceSessionId.HasValue)
            {
                return Unauthorized(new { error = "Invalid or missing device session." });
            }

            var result = await _sessionQueryService.GetSessionPollingStatusAsync(tableSessionId, CurrentDeviceSessionId.Value);

            if (result == null)
            {
                return NotFound(new { error = "Session or device not found." });
            }

            return Ok(result);
        }
    }
}