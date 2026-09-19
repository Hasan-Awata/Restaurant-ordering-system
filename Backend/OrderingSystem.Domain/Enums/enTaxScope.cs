using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Enums
{
    public enum enTaxScope
    {
        PerBill = 1,     // Applied once per total bill (e.g., Delivery Fee)
        PerGuest = 2,    // Multiplied by number of active device sessions (e.g., Cover Charge)
        PerItem = 3      // Multiplied by total quantity of ordered items
    }
}
