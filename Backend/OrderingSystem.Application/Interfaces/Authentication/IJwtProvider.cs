using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace OrderingSystem.Application.Interfaces.Authentication
{
    public interface IJwtProvider
    {
        (string Token, DateTime ExpiryTime) GenerateToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
