using OrderingSystem.Application.Interfaces.Bills;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Infrastructure.Data;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class BillRepository : IBillRepository
    {
        private readonly OrderingSystemDbContext _context;

        public BillRepository(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task AddBillAsync(Bill bill)
        {
            await _context.Bills.AddAsync(bill);
            await _context.SaveChangesAsync();
        }
    }
}