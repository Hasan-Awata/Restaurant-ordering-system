using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.DTOs
{
    public class TaxRecords
    {
        public record AddTaxRequest(string NameAr, string NameEn, decimal Amount, enTaxType TaxType, enTaxScope TaxScope, bool IsActive);
        public record UpdateTaxRequest(int TaxId, string NameAr, string NameEn, decimal Amount, enTaxType TaxType, enTaxScope TaxScope, bool IsActive);
        public record TaxResponse(int TaxId, string NameAr, string NameEn, decimal Amount, enTaxType TaxType, enTaxScope TaxScope, bool IsActive);
    }
}