using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Entities
{
    public class BillItem
    {
        public int BillItemId { get; set; }
        public int BillId { get; set; }
        public int MenuItemId { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string NameEn { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public Bill Bill { get; set; } = null!;
    }
}
