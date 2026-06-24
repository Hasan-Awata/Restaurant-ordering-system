using OrderingSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ITableSessionQuery
    {
        Task<SessionResponse?> GetActiveSessionByTableAsync(int tableId);
    }
}
