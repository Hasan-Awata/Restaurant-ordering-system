using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Required for DbUpdateException

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
            // 1. Log the full exception for debugging
            _logger.LogError(
                exception, "An unhandled exception occurred: {Message}", exception.Message);

            // 2. Set up the default generic response
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server Error",
                Detail = "An unexpected error occurred while processing your request. Please try again later."
            };

            // 3. Catch EF Core database update/constraint exceptions
            if (exception is DbUpdateException dbUpdateEx)
            {
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Database Constraint Violation";

                // The actual PostgreSQL constraint violation message is usually in the InnerException
                problemDetails.Detail = dbUpdateEx.InnerException?.Message ?? dbUpdateEx.Message;
            }

            // 4. Write the response
            httpContext.Response.StatusCode = problemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}