using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.WebApi.Controllers.Base;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
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
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _authCommandService.CreateUserAsync(request);

            return HandleResult(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authCommandService.RefreshTokenAsync(request);
            return HandleResult(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Extract the UserId cleanly using the BaseController property
            if (!CurrentUserId.HasValue)
            {
                return Unauthorized(new { error = "Invalid token claims." });
            }

            var result = await _authCommandService.LogoutAsync(CurrentUserId.Value);
            return HandleResult(result);
        }
    }
}