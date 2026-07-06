using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace OrderingSystem.WebApi.Controllers.Base
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        // ── Property: Secure User Extraction ──────────────────────────────
        protected int? CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                                  User.FindFirst(JwtRegisteredClaimNames.Sub);

                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }
                return null;
            }
        }

        // ── Property: Secure Device Session Extraction ────────────────────
        protected Guid? CurrentDeviceSessionId
        {
            get
            {
                if (Request.Cookies.TryGetValue("DeviceSessionId", out var cookieValue) &&
                    Guid.TryParse(cookieValue, out var deviceSessionId))
                {
                    return deviceSessionId;
                }
                return null;
            }
        }

        // ── Method: Standard Result Mapping (200 OK / 204 No Content) ─────
        protected IActionResult HandleResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Value == null ? NoContent() : Ok(result.Value);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        protected IActionResult HandleResult(Result result)
        {
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        // ── Method: Created Result Mapping (201 CreatedAtAction) ──────────
        protected IActionResult HandleCreatedResult<T>(Result<T> result, string actionName, object routeValues)
        {
            if (result.IsSuccess)
            {
                return CreatedAtAction(actionName, routeValues, result.Value);
            }

            return MapError(result.ErrorType, result.ErrorMessage);
        }

        // ── Helper: Unified Error Mapping ─────────────────────────────────
        private IActionResult MapError(enErrorType errorType, string? errorMessage)
        {
            return errorType switch
            {
                enErrorType.Validation => BadRequest(new { error = errorMessage }),
                enErrorType.NotFound => NotFound(new { error = errorMessage }),
                enErrorType.Conflict => Conflict(new { error = errorMessage }),
                enErrorType.Unauthorized => Unauthorized(new { error = errorMessage }),
                enErrorType.BalanceViolation => UnprocessableEntity(new { error = errorMessage }),
                enErrorType.Failure => StatusCode(StatusCodes.Status500InternalServerError, new { error = errorMessage }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { error = "An unexpected error occurred." })
            };
        }
    }
}