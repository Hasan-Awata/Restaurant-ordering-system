using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;

namespace OrderingSystem.Application.Interfaces.Authentication
{
    public interface IUserQuery
    {
        Task<Result<IEnumerable<UserResponse>>> GetAllUsersAsync();
    }
}