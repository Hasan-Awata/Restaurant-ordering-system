using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Domain.Entities
{
    public class TableSession
    {
        public int SessionId { get; set; }
        public int TableId { get; set; }
        public int WaiterId { get; set; }
        public string SessionToken { get; set; } = string.Empty;
        public enSessionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; } // Nullable since it might not be closed yet

        // Navigation
        public Table Table { get; set; } = new Table();
        public User Waiter { get; set; } = new User();
        public ICollection<SessionDevice> Devices { get; set; } = new List<SessionDevice>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
