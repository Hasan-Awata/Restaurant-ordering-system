using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Auth;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.Services
{
    public class AuthCommandService : IAuthCommandService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtProvider _jwtProvider;

        public AuthCommandService(IUserRepository userRepository, IJwtProvider jwtProvider)
        {
            _userRepository = userRepository;
            _jwtProvider = jwtProvider;
        }

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
        {
            // Note: Hashing should be applied to request.Password before querying
            var user = await _userRepository.GetUserByFullNameAsync(request.FullName);

            if (user == null)
            {
                return Result<LoginResponse>.Failure("Account was not found.", enErrorType.Unauthorized);
            }
            

            if (!BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash)) 
            { 
                return Result<LoginResponse>.Failure("Invalid password.", enErrorType.Unauthorized);
            }
            
            var token = _jwtProvider.GenerateToken(user);
            var response = new LoginResponse(token, user.FullName, user.Role);

            return Result<LoginResponse>.Success(response);
        }

        public async Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request)
        {
            if (await _userRepository.UserExistsAsync(request.FullName))
            {
                return Result<UserResponse>.Failure("User already exists.", enErrorType.Conflict);
            }

            var newUser = new User
            {
                FullName = request.FullName,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(request.Password), 
                Role = request.Role
            };

            await _userRepository.AddUserAsync(newUser);

            var response = new UserResponse(newUser.UserId, newUser.FullName, newUser.Role);
            return Result<UserResponse>.Success(response);
        }
    }
}