using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SmartHome.Api.ErrorHandling {

    /// <summary>
    /// Central exception handler that maps unhandled exceptions to ProblemDetails responses.
    /// <see cref="ArgumentException"/> -> 400 Bad Request, everything else -> 500 Internal Server Error.
    /// Does not log exceptions (see backend issue #2 for structured logging) and never exposes
    /// stack traces or other internal details in the response body.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler {
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) {
            var isArgumentException = exception is ArgumentException;

            var problemDetails = new ProblemDetails {
                Status = isArgumentException ? StatusCodes.Status400BadRequest : StatusCodes.Status500InternalServerError,
                Title = isArgumentException ? "Bad Request" : "Internal Server Error",
                Detail = isArgumentException ? exception.Message : "An unexpected error occurred."
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
