using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Vacancy.Api.Messaging.RabbitMq;

namespace Vacancy.Api.Middleware;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not HhJobQueueUnavailableException)
        {
            return false;
        }

        logger.LogWarning(exception, "Failed to publish HH parse job to RabbitMQ.");

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Job queue unavailable",
            Detail = "The parsing queue is temporarily unavailable.",
            Instance = httpContext.Request.Path
        }, cancellationToken);

        return true;
    }
}
