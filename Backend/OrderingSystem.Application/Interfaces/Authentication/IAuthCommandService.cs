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
        Task<Result> LogoutAsync(int userId);
        Task<Result<UserResponse>> CreateUserAsync(CreateUserRequest request);
        Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request);
    }
}
