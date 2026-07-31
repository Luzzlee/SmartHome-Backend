using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace SmartHome.Shared.UnitTests {

    [TestFixture]
    public class MqttHelperUnitTest {
        private MqttHelper _mqttHelper = null!;

        [SetUp]
        public void Setup() {
            var hubContextMock = new Mock<IHubContext<DeviceHub>>();
            var configurationMock = new Mock<IConfiguration>();
            _mqttHelper = new MqttHelper(hubContextMock.Object, configurationMock.Object, NullLogger<MqttHelper>.Instance);
        }

        [Test]
        public void IsConnected_IsFalse_BeforeConnectAsyncHasBeenCalled() {
            // Guards the /health MQTT check: a freshly constructed MqttHelper (e.g. broker unreachable
            // at startup, ConnectAsync() never having succeeded) must report as disconnected rather than
            // throwing or defaulting to "connected".
            Assert.That(_mqttHelper.IsConnected, Is.False);
        }
    }
}
