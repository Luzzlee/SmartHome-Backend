using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartHome.Api.HealthChecks;
using SmartHome.Shared;

namespace SmartHome.Api.UnitTests.HealthChecks {

    [TestFixture]
    public class DatabaseHealthCheckUnitTest {
        [Test]
        public async Task CheckHealthAsync_ReturnsHealthy_WhenDatabaseIsReachable() {
            var dbConnectionFactory = new DbConnectionFactory("Data Source=:memory:");
            var healthCheck = new DatabaseHealthCheck(dbConnectionFactory);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Healthy));
        }

        [Test]
        public async Task CheckHealthAsync_ReturnsUnhealthy_WhenDatabaseIsNotReachable() {
            // Mode=ReadOnly against a database file that doesn't exist fails to open rather than
            // creating it, which is a reliable way to simulate an unreachable database in a unit test.
            var dbConnectionFactory = new DbConnectionFactory(@"Data Source=D:\__definitely_missing_dir_for_health_check_test__\health.db;Mode=ReadOnly");
            var healthCheck = new DatabaseHealthCheck(dbConnectionFactory);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }
    }
}
