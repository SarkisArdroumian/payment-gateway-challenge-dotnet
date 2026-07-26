using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentGateway.Api.Infrastructure.Banking;

namespace PaymentGateway.Api.ExceptionHandling;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = httpContext.TraceIdentifier;

        var problemDetails = exception switch
        {
            AcquiringBankUnavailableException => new ProblemDetails
            {
                Title = "Acquiring bank unavailable",
                Status = StatusCodes.Status503ServiceUnavailable
            },
            _ => new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Status = StatusCodes.Status500InternalServerError
            }
        };

        problemDetails.Extensions["traceId"] = traceId;

        _logger.LogError(
            exception,
            "Handling exception for request {Method} {Path}. Responding with status code {StatusCode}. TraceId: {TraceId}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            problemDetails.Status ?? StatusCodes.Status500InternalServerError,
            traceId);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await JsonSerializer.SerializeAsync(httpContext.Response.Body, problemDetails, cancellationToken: cancellationToken);

        return true;
    }
}
