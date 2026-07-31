using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartHome.Shared;

namespace SmartHome.Api.HealthChecks {

    /// <summary>
    /// Reports whether the SQLite database is reachable by opening a connection and running a trivial query.
    /// </summary>
    public class DatabaseHealthCheck : IHealthCheck {
        private readonly DbConnectionFactory _dbConnectionFactory;

        public DatabaseHealthCheck(DbConnectionFactory dbConnectionFactory) {
            _dbConnectionFactory = dbConnectionFactory;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
            try {
                using var connection = _dbConnectionFactory.CreateConnection();
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1;";
                command.ExecuteScalar();

                return Task.FromResult(HealthCheckResult.Healthy("Database connection succeeded."));
            } catch (Exception ex) {
                return Task.FromResult(HealthCheckResult.Unhealthy("Database connection failed.", ex));
            }
        }
    }
}
