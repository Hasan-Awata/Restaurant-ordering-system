using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Authentication;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthCommandService _authCommandService;

        public AuthController(IAuthCommandService authCommandService)
        {
            _authCommandService = authCommandService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authCommandService.LoginAsync(request);
            if (!result.IsSuccess) return Unauthorized(result.ErrorMessage);
            return Ok(result.Value);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _authCommandService.CreateUserAsync(request);
            if (!result.IsSuccess) return BadRequest(result.ErrorMessage);
            return Ok(result.Value);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authCommandService.RefreshTokenAsync(request);

            if (!result.IsSuccess)
                return Unauthorized(result.ErrorMessage);

            return Ok(result.Value);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Extract the UserId from the JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid token claims.");
            }

            var result = await _authCommandService.LogoutAsync(userId);

            if (!result.IsSuccess)
                return BadRequest(result.ErrorMessage);

            return NoContent(); 
        }
    }
}