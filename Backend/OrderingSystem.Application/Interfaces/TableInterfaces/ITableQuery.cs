using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableInterfaces
{
    public interface ITableQuery
    {
        public Task<string?> GetTableQrCodeAsync(int tableId);
    }
}
