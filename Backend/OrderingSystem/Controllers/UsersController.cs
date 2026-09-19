using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.WebApi.Controllers.Base;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize(Policy = "AdminOnly")] 
    public class UsersController : BaseController
    {
        private readonly IUserQuery _userQuery;
        private readonly IAuthCommandService _authCommandService;

        public UsersController(IUserQuery userQuery, IAuthCommandService authCommandService)
        {
            _userQuery = userQuery;
            _authCommandService = authCommandService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            var result = await _userQuery.GetAllUsersAsync();
            if (result.IsSuccess)
            {
                return Ok(new { users = result.Value?.Select(u => new { id = u.UserId, fullName = u.FullName, role = u.Role.ToString() }) });
            }
            return HandleResult(result);
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserRequest request)
        {
            var result = await _authCommandService.UpdateUserAsync(userId, request);
            return HandleResult(result);
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _authCommandService.DeleteUserAsync(userId);
            return HandleResult(result);
        }
    }
}