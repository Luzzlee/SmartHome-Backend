using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SmartHome.Api.HealthChecks {

    /// <summary>
    /// Writes health check results as JSON with a separate status per registered check (e.g. "database",
    /// "mqtt"), so callers of GET /health can tell DB and MQTT connectivity apart instead of getting a
    /// single combined status.
    /// </summary>
    public static class HealthCheckResponseWriter {
        private static readonly JsonSerializerOptions SerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public static Task WriteResponse(HttpContext httpContext, HealthReport healthReport) {
            httpContext.Response.ContentType = "application/json";

            var response = new {
                status = healthReport.Status.ToString(),
                checks = healthReport.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => new {
                        status = entry.Value.Status.ToString(),
                        description = entry.Value.Description
                    }
                )
            };

            return httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
        }
    }
}
