using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Domain.Entities
{
    public class TableSession
    {
        public Guid TableSessionId { get; set; } // Also acts as the secure token for the table session
        public int TableId { get; set; }
        public enSessionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; } // Nullable since it might not be closed yet

        // Navigation Properties
        // Using null! tells the compiler EF Core will handle populating this when loaded
        public Table Table { get; set; } = null!;

        // Initializing collections is correct and prevents NullReferenceExceptions
        public ICollection<DeviceSession> Devices { get; set; } = new List<DeviceSession>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
