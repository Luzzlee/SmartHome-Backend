using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartHome.Api.HealthChecks;
using SmartHome.Shared;

namespace SmartHome.Api.UnitTests.HealthChecks {

    [TestFixture]
    public class MqttHealthCheckUnitTest {
        [Test]
        public async Task CheckHealthAsync_ReturnsUnhealthy_WhenMqttClientIsNotConnected() {
            var hubContextMock = new Mock<IHubContext<DeviceHub>>();
            var configurationMock = new Mock<IConfiguration>();
            var mqttHelper = new MqttHelper(hubContextMock.Object, configurationMock.Object, NullLogger<MqttHelper>.Instance);
            var healthCheck = new MqttHealthCheck(mqttHelper);

            var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

            Assert.That(result.Status, Is.EqualTo(HealthStatus.Unhealthy));
        }
    }
}
