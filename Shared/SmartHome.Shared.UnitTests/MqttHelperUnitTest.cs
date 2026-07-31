using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MQTTnet;
using MQTTnet.Packets;

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

        [Test]
        public void IsSubscribedTo_ReturnsFalse_ForTopicThatWasNeverSubscribed() {
            Assert.That(_mqttHelper.IsSubscribedTo("smarthome/light/kitchen/20250829-001"), Is.False);
        }

        [Test]
        public void UnsubscribeAsync_Throws_AndLeavesTopicUntracked_WhenClientIsNotConnected() {
            // No ConnectAsync() call was made (same premise as IsConnected_IsFalse... above), so the
            // broker round-trip fails. The topic was never tracked as subscribed to begin with, and
            // must stay that way - a failed unsubscribe must not be mistaken for a successful one.
            var topic = "smarthome/light/kitchen/20250829-001";

            Assert.CatchAsync<Exception>(async () => await _mqttHelper.UnsubscribeAsync(topic));
            Assert.That(_mqttHelper.IsSubscribedTo(topic), Is.False);
        }

        [Test]
        public async Task ForgetSubscription_RemovesATrackedTopic_WithoutCallingTheBroker() {
            // Regression test for the delete-device ordering bug: when a broker-side UnsubscribeAsync
            // call fails (e.g. mid-reconnect), the caller falls back to ForgetSubscription so the topic
            // isn't replayed by ResubscribeAllAsync later - this must work purely locally, without
            // itself requiring another (successful) broker round-trip.
            var mqttClientMock = new Mock<IMqttClient>();
            mqttClientMock
                .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MqttClientSubscribeResult(0, new List<MqttClientSubscribeResultItem>(), string.Empty, new List<MqttUserProperty>()));

            var hubContextMock = new Mock<IHubContext<DeviceHub>>();
            var configurationMock = new Mock<IConfiguration>();
            var mqttHelper = new MqttHelper(hubContextMock.Object, configurationMock.Object, NullLogger<MqttHelper>.Instance, mqttClientMock.Object);

            var topic = "smarthome/light/kitchen/20250829-001";
            await mqttHelper.SubscribeAsync(topic);
            Assert.That(mqttHelper.IsSubscribedTo(topic), Is.True);

            mqttHelper.ForgetSubscription(topic);

            Assert.That(mqttHelper.IsSubscribedTo(topic), Is.False);
            mqttClientMock.Verify(c => c.UnsubscribeAsync(It.IsAny<MqttClientUnsubscribeOptions>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
