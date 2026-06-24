using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums; 

namespace OrderingSystem.Domain.Entities
{
    public class SessionDevice
    {
        public int DeviceId { get; set; }
        public int SessionId { get; set; }
        public string DeviceToken { get; set; } = string.Empty;
        public enDeviceRole Role { get; set; }
        public bool IsApproved { get; set; }

        // Navigation
        public TableSession Session { get; set; } = new TableSession();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
