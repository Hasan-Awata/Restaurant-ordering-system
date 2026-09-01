using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TaxesInterfaces
{
    public interface ITaxQuery
    {
        Task<Result<PagedResponse<TaxRecords.TaxResponse>>> GetAllTaxesAsync(PageDTO page);
        Task<Result<TaxRecords.TaxResponse>> GetTaxByIdAsync(int taxId);
    }
}
