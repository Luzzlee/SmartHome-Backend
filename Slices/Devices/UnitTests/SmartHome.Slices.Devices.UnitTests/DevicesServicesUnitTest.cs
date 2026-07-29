using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Moq;
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
            var mqttHelper = new MqttHelper(hubContextMock.Object, configurationMock.Object);
            _service = new DevicesService(_repositoryMock.Object, mqttHelper);
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
                        throw new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: Devices.Id'.", 19);
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
        public void CreateDevice_ShouldThrow_WhenIdCollisionPersistsAfterMaxAttempts() {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            _repositoryMock.Setup(r => r.GetIdOfLatestEntry()).ReturnsAsync($"{today}-001");
            _repositoryMock.Setup(r => r.CreateDevice(It.IsAny<Device>()))
                .ThrowsAsync(new SqliteException("SQLite Error 19: 'UNIQUE constraint failed: Devices.Id'.", 19));

            var device = new Device { Name = "Attic Light", Type = "Light", IpAddress = "192.168.0.12", Active = true };

            Assert.ThrowsAsync<InvalidOperationException>(async () => await _service.CreateDevice(device));
        }
    }
}
