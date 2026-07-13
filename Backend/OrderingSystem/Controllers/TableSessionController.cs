using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrderingSystem.WebApi.Controllers.Base;
using System;
using System.Threading.Tasks;

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
        public async Task<IActionResult> ProcessQrCode([FromBody] ProcessQrCodeRequest request)
        {
            // Use the secure cookie value extracted by the BaseController
            var result = await _sessionCommandService.ProcessTableQrCodeAsync(request.qrCode, CurrentDeviceSessionId);

            // If successful, set/refresh the secure cookie
            if (result.IsSuccess && result.Value?.DeviceSession != null)
            {
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddHours(4)
                };

                Response.Cookies.Append(
                    "DeviceSessionId",
                    result.Value.DeviceSession.DeviceSessionId.ToString(),
                    cookieOptions
                );
            }

            return HandleResult(result);
        }

        // ── CASHIER PATH: Approve the activation request ────────────────────────
        [Authorize(Roles = "Admin,Cashier")]
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
            var result = await _sessionCommandService.ApproveJoiningRequestAsync(request);
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

            var result = await _sessionCommandService.RequestBillAsync(request.TableSessionId, CurrentDeviceSessionId.Value);
            return HandleResult(result);
        }

        // ── CASHIER PATH: Approve the bill ──────────────────────────────────────
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("approve-bill")]
        public async Task<IActionResult> ApproveBill([FromBody] ApproveBillRequest request)
        {
            var result = await _sessionCommandService.ApproveBillAsync(request.TableSessionId);
            return HandleResult(result);
        }

        // ── CASHIER PATH: Close session after payment ───────────────────────────
        [Authorize(Roles = "Admin,Cashier")]
        [HttpPost("end")]
        public async Task<IActionResult> EndTableSession([FromBody] ActivateTableSessionRequest request)
        {
            // Reusing ActivateTableSessionRequest since it only contains the TableSessionId
            var result = await _sessionCommandService.EndTableSessionAsync(request.tableSessionId);
            return HandleResult(result);
        }

        // 2. READ ENDPOINT (Query Path)
        [Authorize(Roles = "Admin,Cashier")]
        [HttpGet("active/{tableId}")]
        public async Task<IActionResult> GetActiveSession(int tableId)
        {
            // Keeping standard Ok/NotFound since the Query returns a raw response, not Result<T>
            var response = await _sessionQueryService.GetActiveSessionByTableAsync(tableId);
            if (response == null) return NotFound(new { error = $"No active session found for table ID {tableId}." });

            return Ok(response);
        }
    }
}