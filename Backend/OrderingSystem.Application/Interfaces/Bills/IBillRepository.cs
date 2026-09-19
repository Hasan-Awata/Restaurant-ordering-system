using OrderingSystem.Domain.Entities;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Interfaces.Bills
{
    public interface IBillRepository
    {
        public Task AddBillAsync(Bill bill);
    }
}