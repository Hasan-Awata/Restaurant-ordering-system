using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;

public class TaxCommandService : ITaxCommandService
{
    private readonly ITaxRepository _taxRepository;
    public TaxCommandService(ITaxRepository taxRepository) => _taxRepository = taxRepository;

    public async Task<Result<TaxRecords.TaxResponse>> AddTaxAsync(TaxRecords.AddTaxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NameEn) || string.IsNullOrWhiteSpace(request.NameAr))
            return Result<TaxRecords.TaxResponse>.Failure("Names cannot be empty.");
        if (request.Amount < 0)
            return Result<TaxRecords.TaxResponse>.Failure("Amount cannot be negative.");

        if (await _taxRepository.TaxExistsByNameAsync(request.NameEn, request.NameAr))
            return Result<TaxRecords.TaxResponse>.Failure("A tax with this name already exists.", enErrorType.Conflict);

        var tax = new Tax { NameAr = request.NameAr, NameEn = request.NameEn, Amount = request.Amount, TaxType = request.TaxType, TaxScope = request.TaxScope, IsActive = request.IsActive };
        await _taxRepository.AddTaxAsync(tax);

        return Result<TaxRecords.TaxResponse>.Success(new TaxRecords.TaxResponse(tax.TaxId, tax.NameAr, tax.NameEn, tax.Amount, tax.TaxType, tax.TaxScope, tax.IsActive));
    }

    public async Task<Result<TaxRecords.TaxResponse>> UpdateTaxAsync(TaxRecords.UpdateTaxRequest request)
    {
        var tax = await _taxRepository.GetTaxByIdAsync(request.TaxId);
        if (tax == null) return Result<TaxRecords.TaxResponse>.Failure("Tax not found.", enErrorType.NotFound);

        tax.NameAr = request.NameAr;
        tax.NameEn = request.NameEn;
        tax.Amount = request.Amount;
        tax.TaxType = request.TaxType;
        tax.TaxScope = request.TaxScope;
        tax.IsActive = request.IsActive;

        await _taxRepository.UpdateTaxAsync(tax);
        return Result<TaxRecords.TaxResponse>.Success(new TaxRecords.TaxResponse(tax.TaxId, tax.NameAr, tax.NameEn, tax.Amount, tax.TaxType, tax.TaxScope, tax.IsActive));
    }

    public async Task<Result<bool>> DeleteTaxAsync(int taxId)
    {
        var tax = await _taxRepository.GetTaxByIdAsync(taxId);
        if (tax == null) return Result<bool>.Failure("Tax not found.", enErrorType.NotFound);

        tax.IsDeleted = true;
        tax.IsActive = false;
        await _taxRepository.UpdateTaxAsync(tax);

        return Result<bool>.Success(true);
    }
}