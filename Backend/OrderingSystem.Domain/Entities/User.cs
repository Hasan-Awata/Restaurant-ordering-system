using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Domain.Entities
{
    public class User
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public enRoleType Role { get; set; }
    }
}
