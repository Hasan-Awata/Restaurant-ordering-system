using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TaxesInterfaces
{
    public interface ITaxRepository
    {
        Task AddTaxAsync(Tax tax);
        Task UpdateTaxAsync(Tax tax);
        Task<Tax?> GetTaxByIdAsync(int taxId);
        Task<bool> TaxExistsByNameAsync(string nameEn, string nameAr);
    }
}
