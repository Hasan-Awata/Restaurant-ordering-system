using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Interfaces.Auth
{
    public interface IUserRepository
    {
        Task<User?> GetUserByFullNameAsync(string fullName);
        Task<bool> UserExistsAsync(string fullName);
        Task AddUserAsync(User user);
    }
}