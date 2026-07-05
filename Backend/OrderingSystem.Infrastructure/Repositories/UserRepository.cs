using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.Interfaces.Auth;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Infrastructure.Data;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly OrderingSystemDbContext _context;

        public UserRepository(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByFullNameAsync(string fullName)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.FullName == fullName);
        }

        public async Task<bool> UserExistsAsync(string fullName)
        {
            return await _context.Users.AnyAsync(u => u.FullName == fullName);
        }

        public async Task AddUserAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
        }
    }
}