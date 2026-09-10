using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Interfaces.Auth
{
    public interface IUserRepository
    {
        Task<User?> GetUserByFullNameAsync(string fullName);
        Task<User?> GetUserByIdAsync(int userId); 
        Task<bool> UserExistsAsync(string fullName);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task DeleteUserAsync(User user);
    }
}