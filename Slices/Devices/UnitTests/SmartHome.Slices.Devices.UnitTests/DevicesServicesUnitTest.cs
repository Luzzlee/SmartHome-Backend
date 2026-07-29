using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
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
    }
}
