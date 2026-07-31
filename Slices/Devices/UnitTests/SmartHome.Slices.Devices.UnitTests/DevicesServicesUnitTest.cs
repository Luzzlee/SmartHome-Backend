using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MQTTnet;
using MQTTnet.Packets;
using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using SmartHome.Slices.Devices.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SmartHome.Slices.Devices.UnitTests {

    [TestFixture]
    public class DevicesServicesUnitTest {
        private Mock<IDevicesRepository> _repositoryMock = null!;
        private DevicesService _service = null!;

        [SetUp]
        public void Setup() {
            _repositoryMock = new Mock<IDevicesRepository>();
            var hubContextMock = new Mock<IHubContext<DeviceHub>>();
            var configurationMock = new Mock<IConfiguration>();
            var mqttHelper = new MqttHelper(hubContextMock.Object, configurationMock.Object, NullLogger<MqttHelper>.Instance);
            _service = new DevicesService(_repositoryMock.Object, mqttHelper, NullLogger<DevicesService>.Instance);
        }

        [Test]
        public void GetDeviceById_ShouldThrom_WhenIdIsEmpty() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.GetDeviceById(""), "Device ID cannot be empty.");
        }

        [Test]
        public void GetDeviceById_ShouldThrow_WhenIdIsInvalid() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.GetDeviceById("InvalidId"), "Device ID 'InvalidId' does not match pattern. Expected Format: yyyyMMdd-XXX (z. B. 20250829-001)");
        }

        [Test]
        public void GetDeviceById_ShouldThrow_WhenIdDateIsInvalid() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.GetDeviceById("33333333-001"), "Invalid date '33333333' in id '33333333-001'. Expected Format: yyyyMMdd - XXX(z.B. 20250829 - 001)");
        }

        [Test]
        public void SearchDeviceByName_ShouldThrow_WhenNameIsEmpty() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.SearchDeviceByName(""));
        }

        [Test]
        public void CreateDevice_ShouldThrow_WhenNameIsEmpty() {
            var device = new Device { Id = "20240824-001", Name = "", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreateDevice(device));
        }

        [Test]
        public void CreateDevice_ShouldThrow_WhenIpAdressIsInvalid() {
            var device = new Device { Id = "20240824-001", Name = "Living Room Light", Type = "Light", IpAddress = "InvalidIP", Active = true };

            Assert.ThrowsAsync<ArgumentException>(async () => await _service.CreateDevice(device), "Device IP address is not valid.");
        }

        [Test]
        public void UpdateDevice_ShouldThrow_WhenIdIsEmpty() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.SetDeviceActiveStatus("", true), "Device ID cannot be empty.");
        }

        [Test]
        public void UpdateDevice_ShouldThrow_WhenIdIsInvalid() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.SetDeviceActiveStatus("InvalidId", true), "Device ID 'InvalidId' does not match pattern. Expected Format: yyyyMMdd-XXX (z. B. 20250829-001)");
        }

        [Test]
        public void UpdateDevice_ShouldThrow_WhenIdDateIsInvalid() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.SetDeviceActiveStatus("33333333-001", true), "Invalid date '33333333' in id '33333333-001'. Expected Format: yyyyMMdd - XXX(z.B. 20250829 - 001)");
        }

        [Test]
        public async Task CreateDevice_ShouldGenerateFirstId_WhenDatabaseIsEmpty() {
            _repositoryMock.Setup(r => r.GetIdOfLatestEntry()).ReturnsAsync((string?)null);
            _repositoryMock.Setup(r => r.CreateDevice(It.IsAny<Device>()))
                .Returns((Device device) => Task.FromResult<Device?>(device));

            var device = new Device { Name = "Living Room Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            var result = await _service.CreateDevice(device);

            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo($"{today}-001"));
        }

        [Test]
        public async Task CreateDevice_ShouldFallBackToIdOne_WhenTodaysCounterPartIsNotParsable() {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            _repositoryMock.Setup(r => r.GetIdOfLatestEntry()).ReturnsAsync($"{today}-abc");
            _repositoryMock.Setup(r => r.CreateDevice(It.IsAny<Device>()))
                .Returns((Device device) => Task.FromResult<Device?>(device));

            var device = new Device { Name = "Garage Light", Type = "Light", IpAddress = "192.168.0.10", Active = true };

            var result = await _service.CreateDevice(device);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo($"{today}-001"));
        }

        [Test]
        public async Task CreateDevice_ShouldRetryWithNewId_WhenConcurrentInsertCausesIdCollision() {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var createDeviceCallCount = 0;

            // Simulates a concurrent CreateDevice call: the first insert attempt collides with an
            // id that was written by "another request" in between GetIdOfLatestEntry and the insert.
            _repositoryMock.SetupSequence(r => r.GetIdOfLatestEntry())
                .ReturnsAsync($"{today}-001")
                .ReturnsAsync($"{today}-002");

            _repositoryMock.Setup(r => r.CreateDevice(It.IsAny<Device>()))
                .Returns((Device device) => {
                    createDeviceCallCount++;
                    if (createDeviceCallCount == 1) {
                        throw new DeviceIdCollisionException("Device id is already taken.");
                    }
                    return Task.FromResult<Device?>(device);
                });

            var device = new Device { Name = "Hallway Light", Type = "Light", IpAddress = "192.168.0.11", Active = true };

            var result = await _service.CreateDevice(device);

            Assert.That(createDeviceCallCount, Is.EqualTo(2));
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo($"{today}-003"));
        }

        [Test]
        public void DeleteDevice_ShouldThrow_WhenIdIsEmpty() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.DeleteDevice(""), "Device ID cannot be empty.");
        }

        [Test]
        public void DeleteDevice_ShouldThrow_WhenIdIsInvalid() {
            Assert.ThrowsAsync<ArgumentException>(async () => await _service.DeleteDevice("InvalidId"), "Device ID 'InvalidId' does not match pattern. Expected Format: yyyyMMdd-XXX (z. B. 20250829-001)");
        }

        [Test]
        public async Task DeleteDevice_ShouldReturnFalse_WhenDeviceDoesNotExist() {
            _repositoryMock.Setup(r => r.GetDeviceById("20250829-001")).ReturnsAsync((Device?)null);

            var result = await _service.DeleteDevice("20250829-001");

            Assert.That(result, Is.False);
            _repositoryMock.Verify(r => r.DeleteDevice(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task DeleteDevice_ShouldReturnTrue_WhenDeviceExistsAndIsDeleted() {
            var device = new Device { Id = "20250829-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };
            _repositoryMock.Setup(r => r.GetDeviceById("20250829-001")).ReturnsAsync(device);
            _repositoryMock.Setup(r => r.DeleteDevice("20250829-001")).ReturnsAsync(true);

            var result = await _service.DeleteDevice("20250829-001");

            Assert.That(result, Is.True);
            _repositoryMock.Verify(r => r.DeleteDevice("20250829-001"), Times.Once);
        }

        [Test]
        public async Task DeleteDevice_StillDeletesFromDb_AndForgetsSubscription_WhenMqttUnsubscribeThrows() {
            // Regression test for the delete-device ordering bug: previously the DB row was deleted
            // before the MQTT unsubscribe was attempted, so a broker call that throws (e.g. mid-reconnect,
            // see issue #4's backoff loop) surfaced as an unhandled 500 even though the device was already
            // gone, AND left the topic stuck in MqttHelper's tracked-subscriptions set forever - meaning
            // it would get silently replayed by ResubscribeAllAsync on the next successful reconnect for a
            // device that no longer exists. This test simulates exactly that failure path: the topic *was*
            // subscribed, but the broker-side UnsubscribeAsync call throws.
            var device = new Device { Id = "20250829-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };
            var topic = $"smarthome/{device.Type.ToLower()}/{device.Name.ToLower()}/{device.Id}";

            var mqttClientMock = new Mock<IMqttClient>();
            mqttClientMock
                .Setup(c => c.SubscribeAsync(It.IsAny<MqttClientSubscribeOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MqttClientSubscribeResult(0, new List<MqttClientSubscribeResultItem>(), string.Empty, new List<MqttUserProperty>()));
            mqttClientMock
                .Setup(c => c.UnsubscribeAsync(It.IsAny<MqttClientUnsubscribeOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Broker unreachable (simulated mid-reconnect failure)."));

            var hubContextMock = new Mock<IHubContext<DeviceHub>>();
            var configurationMock = new Mock<IConfiguration>();
            var mqttHelper = new MqttHelper(hubContextMock.Object, configurationMock.Object, NullLogger<MqttHelper>.Instance, mqttClientMock.Object);
            await mqttHelper.SubscribeAsync(topic);
            Assert.That(mqttHelper.IsSubscribedTo(topic), Is.True, "precondition: topic must be tracked as subscribed for this test to exercise the unsubscribe-failure path");

            _repositoryMock.Setup(r => r.GetDeviceById(device.Id)).ReturnsAsync(device);
            _repositoryMock.Setup(r => r.DeleteDevice(device.Id)).ReturnsAsync(true);

            var service = new DevicesService(_repositoryMock.Object, mqttHelper, NullLogger<DevicesService>.Instance);

            bool result = false;
            Assert.DoesNotThrowAsync(async () => result = await service.DeleteDevice(device.Id), "a failed MQTT unsubscribe must not turn into an unhandled 500 - the DB delete is the primary contract.");

            Assert.That(result, Is.True);
            _repositoryMock.Verify(r => r.DeleteDevice(device.Id), Times.Once);
            Assert.That(mqttHelper.IsSubscribedTo(topic), Is.False, "the topic must be forgotten locally even though the broker unsubscribe failed, otherwise ResubscribeAllAsync would replay it after the next reconnect for a device that no longer exists.");
        }

        [Test]
        public void CreateDevice_ShouldThrow_WhenIdCollisionPersistsAfterMaxAttempts() {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            _repositoryMock.Setup(r => r.GetIdOfLatestEntry()).ReturnsAsync($"{today}-001");
            _repositoryMock.Setup(r => r.CreateDevice(It.IsAny<Device>()))
                .ThrowsAsync(new DeviceIdCollisionException("Device id is already taken."));

            var device = new Device { Name = "Attic Light", Type = "Light", IpAddress = "192.168.0.12", Active = true };

            Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateDevice(device));
        }
    }
}
