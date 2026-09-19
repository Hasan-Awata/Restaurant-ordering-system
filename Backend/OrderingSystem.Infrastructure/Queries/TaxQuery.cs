using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Infrastructure.Queries
{
    public class TaxQuery : ITaxQuery
    {
        private readonly OrderingSystemDbContext _context;
        public TaxQuery(OrderingSystemDbContext context) => _context = context;

        public async Task<Result<PagedResponse<TaxRecords.TaxResponse>>> GetAllTaxesAsync(PageDTO page)
        {
            var query = _context.Taxes.AsNoTracking();
            var totalRecords = await query.CountAsync();
            var items = await query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize)
                .Select(t => new TaxRecords.TaxResponse(t.TaxId, t.NameAr, t.NameEn, t.Amount, t.TaxType, t.TaxScope, t.IsActive))
                .ToListAsync();

            return Result<PagedResponse<TaxRecords.TaxResponse>>.Success(new PagedResponse<TaxRecords.TaxResponse>(items, totalRecords, page.PageNumber, page.PageSize));
        }

        public async Task<Result<TaxRecords.TaxResponse>> GetTaxByIdAsync(int taxId)
        {
            var tax = await _context.Taxes.AsNoTracking()
                .Where(t => t.TaxId == taxId)
                .Select(t => new TaxRecords.TaxResponse(t.TaxId, t.NameAr, t.NameEn, t.Amount, t.TaxType, t.TaxScope, t.IsActive))
                .FirstOrDefaultAsync();

            return tax == null ? Result<TaxRecords.TaxResponse>.Failure("Tax not found.", enErrorType.NotFound) : Result<TaxRecords.TaxResponse>.Success(tax);
        }
    }
}
