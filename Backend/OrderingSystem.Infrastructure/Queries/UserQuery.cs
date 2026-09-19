using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.Domain.Common;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Queries
{
    public class UserQuery : IUserQuery
    {
        private readonly OrderingSystemDbContext _context;

        public UserQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Result<IEnumerable<UserResponse>>> GetAllUsersAsync()
        {
            var users = await _context.Users
                .AsNoTracking()
                .Select(u => new UserResponse(u.UserId, u.FullName, u.Role))
                .ToListAsync();

            return Result<IEnumerable<UserResponse>>.Success(users);
        }
    }
}