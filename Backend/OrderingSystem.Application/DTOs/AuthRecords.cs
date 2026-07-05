using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
        public record LoginRequest(string FullName, string Password);
        public record LoginResponse(string Token, string refreshToken, DateTime AccessTokenExpiry, string FullName, enRoleType Role);
        public record CreateUserRequest(string FullName, string Password, enRoleType Role);
        public record UserResponse(int UserId, string FullName, enRoleType Role);
        public record RefreshTokenRequest(string AccessToken, string RefreshToken);
}
