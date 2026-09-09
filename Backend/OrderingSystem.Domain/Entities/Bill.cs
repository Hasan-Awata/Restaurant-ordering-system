using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Entities
{
    public class Bill
    {
        public int BillId { get; set; }
        public Guid TableSessionId { get; set; }
        public decimal TotalSubTotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal GrandTotal { get; set; }
        public DateTime CreatedAt { get; set; }

        public TableSession TableSession { get; set; } = null!;
        public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
        public ICollection<BillTax> BillTaxes { get; set; } = new List<BillTax>();
    }
}
