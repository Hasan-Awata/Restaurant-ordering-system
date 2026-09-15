using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderingSystem.Application.Interfaces.Data;
using System;

namespace OrderingSystem.Infrastructure.Data
{
    public class PostgresErrorMapper : IDatabaseErrorMapper
    {
        public string? MapDbUpdateException(Exception exception)
        {
            if (exception is DbUpdateException dbUpdateEx && dbUpdateEx.InnerException is PostgresException pgEx)
            {
                return pgEx.SqlState switch
                {
                    PostgresErrorCodes.UniqueViolation => "A record with this information already exists.",
                    PostgresErrorCodes.ForeignKeyViolation => "This operation failed because the record is either currently in use or references missing data.",
                    PostgresErrorCodes.CheckViolation => "The provided data violates a business rule constraint.",
                    PostgresErrorCodes.NotNullViolation => "A required piece of information was missing.",
                    _ => "A database constraint was violated."
                };
            }
            return null;
        }

        public bool IsConnectionError(Exception exception)
        {
            return exception is NpgsqlException;
        }
    }
}