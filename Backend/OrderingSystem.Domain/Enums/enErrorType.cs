using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Domain.Enums
{
    public enum enErrorType
    {
        None = 0,
        Validation = 400,
        NotFound = 404,
        Conflict = 409,
        Unauthorized = 401,
        BalanceViolation = 422,
        Failure = 500
    }
}
