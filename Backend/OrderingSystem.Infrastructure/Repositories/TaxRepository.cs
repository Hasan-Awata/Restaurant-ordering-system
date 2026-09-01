using Microsoft.EntityFrameworkCore;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
namespace OrderingSystem.Infrastructure.Repositories
{
    public class TaxRepository : ITaxRepository
    {
        private readonly OrderingSystemDbContext _context;
        public TaxRepository(OrderingSystemDbContext context) => _context = context;

        public async Task AddTaxAsync(Tax tax)
        {
            _context.Taxes.Add(tax);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTaxAsync(Tax tax)
        {
            _context.Taxes.Update(tax);
            await _context.SaveChangesAsync();
        }

        public async Task<Tax?> GetTaxByIdAsync(int taxId) =>
            await _context.Taxes.FirstOrDefaultAsync(t => t.TaxId == taxId);

        public async Task<bool> TaxExistsByNameAsync(string nameEn, string nameAr) =>
            await _context.Taxes.AnyAsync(t => t.NameEn.ToLower() == nameEn.ToLower() || t.NameAr.ToLower() == nameAr.ToLower());
    }
}
