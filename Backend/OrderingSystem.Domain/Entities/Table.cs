using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Domain.Entities
{
    public class Table
    {
        public int TableId { get; set; }
        public int TableNumber { get; set; }
        public int FloorNumber { get; set; }
        public string QrCode { get; set; } = string.Empty;
        public enTableStatus Status { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation
        public ICollection<TableSession> Sessions { get; set; } = new List<TableSession>();
    }
}
