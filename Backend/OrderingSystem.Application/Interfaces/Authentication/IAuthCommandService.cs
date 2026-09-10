using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.Authentication
{
    public interface IAuthCommandService
    {
        Task<Result<LoginResponse>> LoginAsync(LoginRequest request);
        Task<Result> LogoutAsync(int userId, string token);
        Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request);
        Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
        Task<Result<UserResponse>> UpdateUserAsync(int userId, UpdateUserRequest request);
        Task<Result<bool>> DeleteUserAsync(int userId);
        Task<Result<bool>> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    }
}
