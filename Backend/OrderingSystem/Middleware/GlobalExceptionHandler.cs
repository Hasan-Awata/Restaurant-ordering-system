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
                    detail = dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message;
                    _logger.LogWarning(dbUpdateEx, "Database constraint violation.");
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
                case PostgresException pgEx:
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