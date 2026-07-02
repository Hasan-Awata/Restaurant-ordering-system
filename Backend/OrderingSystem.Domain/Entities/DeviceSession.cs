using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums; 

namespace OrderingSystem.Domain.Entities
{
    public class DeviceSession
    {
        public Guid DeviceSessionId { get; set; } // Also acts as the secure token for the device session
        public Guid TableSessionId { get; set; }
        public enDeviceRole Role { get; set; }
        public bool IsApproved { get; set; }

        // Navigation Properties
        public TableSession TableSession { get; set; } = null!; // Avoid = new TableSession() to prevent EF Core from creating a new database row
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
