using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Auth;
using OrderingSystem.Application.Interfaces.Authentication;

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
    }
}