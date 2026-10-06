using EducationalCenter.Application.Common.Exceptions;
using EducationalCenter.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EducationalCenter.API.Middleware;

/// <summary>
/// The one place that turns exceptions into HTTP answers. Expected business errors keep their message;
/// anything unexpected becomes a generic 500 (the details go to the log, never to the client).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const string GenericMessage = "An unexpected error occurred.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The client gave up waiting: nothing to answer.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = 499;
            return true;
        }

        var (status, title, detail, errors) = Map(exception);

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception. TraceId {TraceId}", httpContext.TraceIdentifier);
        else
            logger.LogInformation("Request rejected with {Status}: {Message}", status, exception.Message);

        ProblemDetails problem = errors is null
            ? new ProblemDetails { Status = status, Title = title, Detail = detail }
            : new ValidationProblemDetails(errors) { Status = status, Title = title, Detail = detail };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }

    private static (int Status, string Title, string Detail, IDictionary<string, string[]>? Errors) Map(Exception exception) =>
        exception switch
        {
            RequestValidationException validation =>
                (StatusCodes.Status400BadRequest, "Validation failed", "One or more validation errors occurred.",
                    validation.Errors.ToDictionary(e => e.Key, e => e.Value)),

            NotFoundException => (StatusCodes.Status404NotFound, "Not found", exception.Message, null),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message, null),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message, null),
            FeatureUnavailableException => (StatusCodes.Status501NotImplemented, "Not available", exception.Message, null),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request", bad.Message, null),

            // Two people saved something that must be unique at the same moment.
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
                (StatusCodes.Status409Conflict, "Conflict", "This value already exists.", null),

            _ => (StatusCodes.Status500InternalServerError, "Server error", GenericMessage, null)
        };
}
