using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using System.Collections.Generic;

namespace OrderingSystem.Application.Interfaces.TaxesInterfaces
{
    public interface ITaxCalculationService
    {
        (List<AppliedTaxResponse> AppliedTaxes, decimal TotalTaxAmount) CalculateTaxes(
            decimal subTotal,
            int uniqueGuestsCount,
            int totalItemsCount,
            IEnumerable<Tax> activeTaxes);
    }
}