using System;

namespace OrderingSystem.Application.Interfaces.Data
{
    public interface IDatabaseErrorMapper
    {
        string? MapDbUpdateException(Exception exception);
        bool IsConnectionError(Exception exception);
    }
}