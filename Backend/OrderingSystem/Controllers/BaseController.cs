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
            var statusCode = errorType switch
            {
                enErrorType.Validation => StatusCodes.Status400BadRequest,
                enErrorType.NotFound => StatusCodes.Status404NotFound,
                enErrorType.Conflict => StatusCodes.Status409Conflict,
                enErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
                enErrorType.BalanceViolation => StatusCodes.Status422UnprocessableEntity,
                enErrorType.Failure => StatusCodes.Status500InternalServerError,
                _ => StatusCodes.Status500InternalServerError
            };

            var title = errorType switch
            {
                enErrorType.Validation => "Validation Error",
                enErrorType.NotFound => "Resource Not Found",
                enErrorType.Conflict => "Conflict",
                enErrorType.Unauthorized => "Unauthorized",
                enErrorType.BalanceViolation => "Business Rule Violation",
                enErrorType.Failure => "Server Error",
                _ => "An unexpected error occurred"
            };

            // The Problem() method automatically formats the response as a standard application/problem+json ProblemDetails object
            return Problem(
                statusCode: statusCode,
                title: title,
                detail: errorMessage
            );
        }
    }
}