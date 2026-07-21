using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Auth;
using OrderingSystem.Application.Interfaces.Authentication;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
namespace OrderingSystem.Application.Services
{
    public class AuthCommandService : IAuthCommandService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtProvider _jwtProvider;
        private readonly IMemoryCache _cache;

        public AuthCommandService(IUserRepository userRepository, IJwtProvider jwtProvider, IMemoryCache cache)
        {
            _userRepository = userRepository;
            _jwtProvider = jwtProvider;
            _cache = cache;
        }

        public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetUserByFullNameAsync(request.FullName);

            if (user == null || !BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash))
            {
                return Result<LoginResponse>.Failure("Invalid credentials.", enErrorType.Unauthorized);
            }

            var (token, expiryTime) = _jwtProvider.GenerateToken(user);
            var refreshToken = _jwtProvider.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUserAsync(user);

            var response = new LoginResponse(token, refreshToken, expiryTime, user.FullName, user.Role);
            return Result<LoginResponse>.Success(response);
        }

        public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var principal = _jwtProvider.GetPrincipalFromExpiredToken(request.AccessToken);

            if (principal == null)
                return Result<LoginResponse>.Failure("Invalid access token.", enErrorType.Unauthorized);

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier) ?? principal.FindFirst(JwtRegisteredClaimNames.Sub);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Result<LoginResponse>.Failure("Invalid token claims.", enErrorType.Unauthorized);

            var user = await _userRepository.GetUserByIdAsync(userId);

            if (user == null ||
                user.RefreshToken != request.RefreshToken ||
                user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Result<LoginResponse>.Failure("Invalid or expired refresh token.", enErrorType.Unauthorized);
            }

            if (user.AbsoluteRefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                // Force them to log in again
                user.RefreshToken = null;
                await _userRepository.UpdateUserAsync(user);

                return Result<LoginResponse>.Failure("Session expired. Please log in again.", enErrorType.Unauthorized);
            }

            // Generate new rotated tokens
            var (newAccessToken, expiryTime) = _jwtProvider.GenerateToken(user);
            var newRefreshToken = _jwtProvider.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userRepository.UpdateUserAsync(user);

            var response = new LoginResponse(newAccessToken, newRefreshToken, expiryTime, user.FullName, user.Role);
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

        public async Task<Result> LogoutAsync(int userId, string token)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null) return Result.Failure("User not found.", enErrorType.NotFound);

            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.AbsoluteRefreshTokenExpiryTime = null;
            await _userRepository.UpdateUserAsync(user);

            // Add the token to the blacklist cache for 15 minutes (matching token lifespan)
            _cache.Set($"blacklist_{token}", true, TimeSpan.FromMinutes(15));

            return Result.Success();
        }
    }
}