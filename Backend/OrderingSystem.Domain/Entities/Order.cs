using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Domain.Entities
{
    public class Order
    {
        public int OrderId { get; set; }
        public int SessionId { get; set; }
        public int DeviceId { get; set; }
        public decimal TotalAmount { get; set; }
        public enOrderStatusType OrderStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public TableSession Session { get; set; }
        public DeviceSession Device { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
