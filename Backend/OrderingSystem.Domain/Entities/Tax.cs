using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Entities
{
    public class Tax
    {
        public int TaxId { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
        public decimal Amount { get; set; } // Can be a flat amount (e.g., 5.00) or percentage (e.g., 15.00)
        public enTaxType TaxType { get; set; }
        public enTaxScope TaxScope { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
