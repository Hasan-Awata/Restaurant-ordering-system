using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Interfaces.Authentication
{
    public interface IJwtProvider
    {
        string GenerateToken(User user);
    }
}
