using System;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Interfaces.Data
{
    public interface IUnitOfWork
    {
        Task ExecuteTransactionAsync(Func<Task> action);
    }
}