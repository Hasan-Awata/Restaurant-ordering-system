using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql; // Required to catch PostgreSQL-specific exceptions

namespace OrderingSystem.WebApi.Middleware
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // 1. Define default response values for unexpected errors
            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "Server Error";
            string detail = "An unexpected error occurred while processing your request. Please try again later.";

            // 2. Map the exception to the appropriate HTTP status and message
            switch (exception)
            {
                // Caught FIRST: Concurrency conflicts (Two users editing the same record)
                case DbUpdateConcurrencyException concurrencyEx:
                    statusCode = StatusCodes.Status409Conflict;
                    title = "Concurrency Conflict";
                    detail = "The record you attempted to edit was modified by another process. Please refresh and try again.";
                    _logger.LogWarning(concurrencyEx, "Concurrency conflict detected.");
                    break;

                // Caught SECOND: General database constraint violations (e.g., Foreign Key deletion failures)
                case DbUpdateException dbUpdateEx:
                    statusCode = StatusCodes.Status409Conflict;
                    title = "Database Constraint Violation";

                    // 1. Check if the underlying database threw a Postgres-specific exception
                    if (dbUpdateEx.InnerException is PostgresException pgEx)
                    {
                        // 2. Map the specific PostgreSQL state code to a safe, precise business message
                        detail = pgEx.SqlState switch
                        {
                            // 23505: unique_violation
                            PostgresErrorCodes.UniqueViolation => "A record with this information already exists.",

                            // 23503: foreign_key_violation
                            PostgresErrorCodes.ForeignKeyViolation => "This operation failed because the record is either currently in use or references missing data.",

                            // 23514: check_violation
                            PostgresErrorCodes.CheckViolation => "The provided data violates a business rule constraint.",

                            // 23502: not_null_violation
                            PostgresErrorCodes.NotNullViolation => "A required piece of information was missing.",

                            _ => "A database constraint was violated."
                        };

                        // Optional: If you want absolute precision, you can safely map specific constraint names
                        // without exposing them to the client.
                        if (pgEx.ConstraintName == "IX_TableSessions_TableId")
                        {
                            detail = "This table already has an active session.";
                        }

                        // Log the actual sensitive data securely on the server
                        _logger.LogWarning(dbUpdateEx, "Database constraint violation. SqlState: {SqlState}, Constraint: {ConstraintName}", pgEx.SqlState, pgEx.ConstraintName);
                    }
                    else
                    {
                        // Fallback for non-Postgres DbUpdateExceptions
                        detail = "A data conflict occurred while processing your request.";
                        _logger.LogWarning(dbUpdateEx, "Database constraint violation (Non-Postgres).");
                    }
                    break;

                // Caught THIRD: Client disconnected or request timed out
                case OperationCanceledException canceledEx:
                    statusCode = 499; // 499 is the standard Nginx code for "Client Closed Request"
                    title = "Request Canceled";
                    detail = "The client canceled the request before the server could complete it.";

                    // Log as Information instead of Error to avoid cluttering logs with false alarms
                    _logger.LogInformation("Request was canceled by the client.");
                    break;

                // Caught FOURTH: PostgreSQL server connectivity issues
                case NpgsqlException npgsqlEx:
                    statusCode = StatusCodes.Status500InternalServerError;
                    title = "Database Connection Error";
                    detail = "Unable to communicate with the database. Please try again later.";

                    // Log the critical system failure, but hide the details from the user
                    _logger.LogError(exception, "A database connectivity error occurred.");
                    break;

                // DEFAULT: Anything else (NullReferenceExceptions, etc.)
                default:
                    _logger.LogError(exception, "An unhandled exception occurred: {Message}", exception.Message);
                    break;
            }

            // 3. Construct the standardized ProblemDetails object
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            };

            // 4. Write the response back to the client
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}