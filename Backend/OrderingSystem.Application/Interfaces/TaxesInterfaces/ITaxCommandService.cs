using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TaxesInterfaces
{
    public interface ITaxCommandService
    {
        Task<Result<TaxRecords.TaxResponse>> AddTaxAsync(TaxRecords.AddTaxRequest request);
        Task<Result<TaxRecords.TaxResponse>> UpdateTaxAsync(TaxRecords.UpdateTaxRequest request);
        Task<Result<bool>> DeleteTaxAsync(int taxId);
    }
}
