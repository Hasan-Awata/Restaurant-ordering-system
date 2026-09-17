using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using System.Collections.Generic;

namespace OrderingSystem.Application.Interfaces.TaxesInterfaces
{
    public interface ITaxCalculationService
    {
        public (List<AppliedTaxResponse> AppliedTaxes, decimal TotalTaxAmount) CalculateTaxes(
            decimal subTotal,
            int uniqueGuestsCount,
            int totalItemsCount,
            IEnumerable<Tax> activeTaxes);

        public decimal CalculateGuestTaxAmount(decimal guestSubTotal, int guestItemsCount, int totalGuestsCount, IEnumerable<Tax> activeTaxes);
    }
}