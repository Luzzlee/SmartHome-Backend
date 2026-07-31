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
        public void GetDeviceById_WhenServiceThrowsArgumentException_PropagatesException() {
            _serviceMock
                .Setup(s => s.GetDeviceById("invalidId"))
                .ThrowsAsync(new ArgumentException());

            Assert.ThrowsAsync<ArgumentException>(async () => await _controller.GetDeviceById("invalidId"));
        }

        [Test]
        public async Task SearchDeviceByName_WhenDevicesMatch_ShouldReturnOk() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.SearchDeviceByName("Bedroom"))
                .ReturnsAsync(new List<Device> { device });

            var result = await _controller.SearchDeviceByName("Bedroom");

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task SearchDeviceByName_WhenNoDeviceMatches_ShouldReturnOkWithEmptyList() {
            _serviceMock
                .Setup(s => s.SearchDeviceByName("20210824-001"))
                .ReturnsAsync(new List<Device>());

            var result = await _controller.SearchDeviceByName("20210824-001") as OkObjectResult;

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Value, Is.TypeOf<List<Device>>().And.Empty);
        }

        [Test]
        public void SearchDeviceByName_WhenServiceThrowsArgumentException_PropagatesException() {
            _serviceMock
                .Setup(s => s.SearchDeviceByName(""))
                .ThrowsAsync(new ArgumentException());

            Assert.ThrowsAsync<ArgumentException>(async () => await _controller.SearchDeviceByName(""));
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
        public void CreateDevice_WhenServiceThrowsArgumentException_PropagatesException() {
            var device = new Device { Id = "20210824-001", Name = "", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            _serviceMock
                .Setup(s => s.CreateDevice(device))
                .ThrowsAsync(new ArgumentException());

            Assert.ThrowsAsync<ArgumentException>(async () => await _controller.CreateDevice(device));
        }

        [Test]
        public async Task DeleteDevice_WhenDeviceDoesExist_ShouldReturnNoContent() {
            _serviceMock
                .Setup(s => s.DeleteDevice("20210824-001"))
                .ReturnsAsync(true);

            var result = await _controller.DeleteDevice("20210824-001");

            Assert.That(result, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task DeleteDevice_WhenDeviceDoesNotExist_ShouldReturnNotFound() {
            _serviceMock
                .Setup(s => s.DeleteDevice("20210824-001"))
                .ReturnsAsync(false);

            var result = await _controller.DeleteDevice("20210824-001");

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void DeleteDevice_WhenServiceThrowsArgumentException_PropagatesException() {
            _serviceMock
                .Setup(s => s.DeleteDevice("invalidId"))
                .ThrowsAsync(new ArgumentException());

            Assert.ThrowsAsync<ArgumentException>(async () => await _controller.DeleteDevice("invalidId"));
        }
    }
}
