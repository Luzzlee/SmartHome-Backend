using Moq;
using System.Runtime.CompilerServices;
using SmartHome.Slices.Devices.Services;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Shared;

namespace SmartHome.Api.UnitTests {
    
    [TestFixture]
    public class DeviceControllersUnitTest {
        private Mock<IDevicesService> _serviceMock = null!;
        private DevicesController _controller = null!;

        [SetUp]
        public void Setup() {
            _serviceMock = new Mock<IDevicesService>();
            _controller = new DevicesController(_serviceMock.Object);
        }

        [Test]
        public async Task GetAllDevices_ShouldReturnOk() {
            var result = await _controller.GetAllDevices();
            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task GetDeviceById_WhenDeviceDoesExist_ShouldReturnOk() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.GetDeviceById("20210824-001"))
                .ReturnsAsync(device);

            var result = await _controller.GetDeviceById("20210824-001");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task GetDeviceById_WhenDeviceDoesNotExist_ShouldReturnNotFound() {
            _serviceMock
                .Setup(s => s.GetDeviceById("20210824-001"))
                .ReturnsAsync((Device?)null);

            var result = await _controller.GetDeviceById("20210824-001");

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task GetDeviceById_WhenServiceThrowsArgumentException_ReturnBadRequest() {
            _serviceMock
                .Setup(s => s.GetDeviceById("invalidId"))
                .ThrowsAsync(new ArgumentException());

            var result = await _controller.GetDeviceById("invalidId");

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SearchDeviceByName_WhenDeviceDoesExist_ShouldReturnOk() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.SearchDeviceByName("20210824-001"))
                .ReturnsAsync(device);

            var result = await _controller.SearchDeviceByName("20210824-001");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task SearchDeviceByName_WhenDeviceDoesNotExist_ShouldReturnNotFound() {
            _serviceMock
                .Setup(s => s.SearchDeviceByName("20210824-001"))
                .ReturnsAsync((Device?)null);

            var result = await _controller.SearchDeviceByName("20210824-001");

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task SearchDeviceByName_WhenServiceThrowsArgumentException_ReturnBadRequest() {
            _serviceMock
                .Setup(s => s.SearchDeviceByName(""))
                .ThrowsAsync(new ArgumentException());

            var result = await _controller.SearchDeviceByName("");

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateDevice_WhenDeviceWasCreated_ShouldReturnCreated() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.CreateDevice(device))
                .ReturnsAsync(device);

            var result = await _controller.CreateDevice(device);

            Assert.That(result, Is.TypeOf<CreatedAtActionResult>());
        }

        [Test]
        public async Task CreateDevice_WhenDeviceWasNotCreated_ShouldReturnNotFound() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.CreateDevice(device))
                .ReturnsAsync((Device?)null);

            var result = await _controller.CreateDevice(device);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateDevice_WhenServiceThrowsArgumentException_ReturnBadRequest() {
            var device = new Device { Id = "20210824-001", Name = "", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.CreateDevice(device))
                .ThrowsAsync(new ArgumentException());

            var result = await _controller.CreateDevice(device);

            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }
    }
}
