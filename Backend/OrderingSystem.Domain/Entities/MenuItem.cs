using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Entities
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public int CategoryId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ? Emoji { get; set; } 
        public bool IsAvailable { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation
        public Category Category { get; set; } = null!;
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
