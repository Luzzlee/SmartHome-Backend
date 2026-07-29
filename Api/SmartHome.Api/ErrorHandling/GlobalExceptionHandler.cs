using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SmartHome.Api.ErrorHandling {

    /// <summary>
    /// Central exception handler that maps unhandled exceptions to ProblemDetails responses.
    /// <see cref="ArgumentException"/> -> 400 Bad Request, everything else -> 500 Internal Server Error.
    /// Logs every exception it handles before writing the response, and never exposes stack traces
    /// or other internal details in the response body.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
            var isArgumentException = exception is ArgumentException;

            if (isArgumentException) {
                _logger.LogWarning(exception, "Request to {Path} failed with a bad request: {Message}", httpContext.Request.Path, exception.Message);
            } else {
                _logger.LogError(exception, "Unhandled exception while processing request to {Path}.", httpContext.Request.Path);
            }

            var problemDetails = new ProblemDetails {
                Status = isArgumentException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError,
                Title = isArgumentException ? "Bad Request" : "Internal Server Error",
                Detail = isArgumentException ? exception.Message : "An unexpected error occurred."
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken);

            return true;
        }
    }
}
