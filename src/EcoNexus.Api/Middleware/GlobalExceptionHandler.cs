using EcoNexus.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace EcoNexus.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // 1. FluentValidation failures -> 400
        if (exception is ValidationException validationException)
        {
            logger.LogWarning(
                "Validation failed for request {Path}. TraceId: {TraceId}",
                httpContext.Request.Path,
                httpContext.TraceIdentifier);

            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var validationProblem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            };
            validationProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = validationProblem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(validationProblem, cancellationToken);

            return true;
        }

        // 2. Not found -> 404
        if (exception is NotFoundException notFoundException)
        {
            logger.LogWarning(
                "Resource not found: {Message}. TraceId: {TraceId}",
                notFoundException.Message,
                httpContext.TraceIdentifier);

            var notFoundProblem = new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found",
                Detail = notFoundException.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            };
            notFoundProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = notFoundProblem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(notFoundProblem, cancellationToken);

            return true;
        }

        // 3. Optimistic concurrency conflict -> 409
        if (exception is ConcurrencyConflictException concurrencyException)
        {
            logger.LogWarning(
                "Concurrency conflict: {Message}. TraceId: {TraceId}",
                concurrencyException.Message,
                httpContext.TraceIdentifier);

            var concurrencyProblem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = concurrencyException.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            };
            concurrencyProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = concurrencyProblem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(concurrencyProblem, cancellationToken);

            return true;
        }

        // 4. Domain/business conflict -> 409
        if (exception is ConflictException conflictException)
        {
            logger.LogWarning(
                "Conflict: {Message}. TraceId: {TraceId}",
                conflictException.Message,
                httpContext.TraceIdentifier);

            var conflictProblem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflictException.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            };
            conflictProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = conflictProblem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(conflictProblem, cancellationToken);

            return true;
        }

        // 5. Legacy: InvalidOperationException with "already exists" -> 409
        if (exception is InvalidOperationException invalidOp
            && invalidOp.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(
                "Conflict: {Message}. TraceId: {TraceId}",
                invalidOp.Message,
                httpContext.TraceIdentifier);

            var conflictProblem = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = invalidOp.Message,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            };
            conflictProblem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = conflictProblem.Status.Value;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(conflictProblem, cancellationToken);

            return true;
        }

        // 6. Everything else -> 500
        logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}",
            httpContext.TraceIdentifier);

        var response = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
            Detail = "The server encountered an unexpected error."
        };
        response.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = response.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }
}
