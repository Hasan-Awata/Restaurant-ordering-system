using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Entities
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int MenuItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Navigation
        public Order Order { get; set; } = new Order();
        public MenuItem MenuItem { get; set; } = new MenuItem();
    }
}
