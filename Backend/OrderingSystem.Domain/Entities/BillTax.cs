using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Entities
{
    public class BillTax
    {
        public int BillTaxId { get; set; }
        public int BillId { get; set; }
        public string TaxNameAr { get; set; } = string.Empty;
        public string TaxNameEn { get; set; } = string.Empty;
        public decimal AppliedAmount { get; set; }
        public Bill Bill { get; set; } = null!;
    }
}
