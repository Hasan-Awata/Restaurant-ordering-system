using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;

namespace OrderingSystem.Application.Services
{
    public class TaxCalculationService : ITaxCalculationService
    {
        public (List<AppliedTaxResponse> AppliedTaxes, decimal TotalTaxAmount) CalculateTaxes(
            decimal subTotal,
            int uniqueGuestsCount,
            int totalItemsCount,
            IEnumerable<Tax> activeTaxes)
        {
            var appliedTaxes = new List<AppliedTaxResponse>();
            decimal totalTaxAmount = 0m;

            var guestCount = uniqueGuestsCount > 0 ? uniqueGuestsCount : 1;

            foreach (var tax in activeTaxes)
            {
                decimal taxAmount = 0m;

                if (tax.TaxType == enTaxType.Percentage)
                {
                    if (tax.TaxScope == enTaxScope.PerBill)
                        taxAmount = subTotal * (tax.Amount / 100m);
                }
                else if (tax.TaxType == enTaxType.FlatRate)
                {
                    if (tax.TaxScope == enTaxScope.PerBill)
                        taxAmount = tax.Amount;
                    else if (tax.TaxScope == enTaxScope.PerGuest)
                        taxAmount = tax.Amount * guestCount;
                    else if (tax.TaxScope == enTaxScope.PerItem)
                        taxAmount = tax.Amount * totalItemsCount;
                }

                if (taxAmount > 0)
                {
                    var roundedAmount = Math.Round(taxAmount, 2);
                    appliedTaxes.Add(new AppliedTaxResponse(tax.NameEn, tax.NameAr, roundedAmount));
                    totalTaxAmount += roundedAmount;
                }
            }

            return (appliedTaxes, totalTaxAmount);
        }

        public decimal CalculateGuestTaxAmount(decimal guestSubTotal, int guestItemsCount, int totalGuestsCount, IEnumerable<Tax> activeTaxes)
        {
            decimal guestTaxAmount = 0m;
            var guestCount = totalGuestsCount > 0 ? totalGuestsCount : 1;

            foreach (var tax in activeTaxes)
            {
                decimal taxAmount = 0m;
                if (tax.TaxType == enTaxType.Percentage && tax.TaxScope == enTaxScope.PerBill)
                    taxAmount = guestSubTotal * (tax.Amount / 100m); // % of THEIR subtotal
                else if (tax.TaxType == enTaxType.FlatRate)
                {
                    if (tax.TaxScope == enTaxScope.PerBill) taxAmount = tax.Amount / guestCount; // Split flat bill fee
                    else if (tax.TaxScope == enTaxScope.PerGuest) taxAmount = tax.Amount; // Their guest fee
                    else if (tax.TaxScope == enTaxScope.PerItem) taxAmount = tax.Amount * guestItemsCount; // Their items
                }
                guestTaxAmount += Math.Round(taxAmount, 2);
            }
            return guestTaxAmount;
        }
    }
}