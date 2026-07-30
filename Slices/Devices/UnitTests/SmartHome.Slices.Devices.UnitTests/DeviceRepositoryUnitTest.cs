using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;

namespace SmartHome.Slices.Devices.UnitTests {
    [TestFixture]
    public class DeviceRepositoryUnitTest {
        private SqliteTestDatabaseProvider _database;
        private DevicesRepository _repository;

        [SetUp]
        public void Setup() {
            _database = new SqliteTestDatabaseProvider();

            var factory = new DbConnectionFactory(_database.Connection.ConnectionString);
            _repository = new DevicesRepository(factory);
        }

        [TearDown]
        public void TearDown() {
            _database.Dispose();
        }

        [Test]
        public async Task GetAllDevices_ShouldReturnAllDevices() {
            var devices = await _repository.GetAllDevices();

            Assert.That(devices.Count, Is.EqualTo(4));
        }

        [Test]
        public async Task GetDeviceById_ShouldReturnCorrectDevice() {
            var device = await _repository.GetDeviceById("20250605-002");

            Assert.That(device, Is.Not.Null);
            Assert.That(device!.Name, Is.EqualTo("Bathroom Speaker"));
        }

        [Test]
        public async Task SearchDeviceByName_ShouldReturnCorrectDevice() {
            var device = await _repository.SearchDeviceByName("Kitchen Spe%");

            Assert.That(device, Is.Not.Null);
            Assert.That(device!.Id, Is.EqualTo("20250605-001"));
        }

        [Test]
        public async Task CeateDevice_ShouldCreateDatabaseEntry() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            var createdDevice = await _repository.CreateDevice(device);

            Assert.That(createdDevice, Is.Not.Null);
            Assert.That(createdDevice!.Id, Is.EqualTo(device.Id));
        }

        [Test]
        public async Task SetDeviceActiveStatus_ShouldSetActiveStatus() {
            var device = new Device { Id = "20210824-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            var createdDevice = await _repository.CreateDevice(device);
            var updatedDevice = await _repository.SetDeviceActiveStatus("20210824-001", false);
            
            Assert.That(createdDevice, Is.Not.Null);
            Assert.That(updatedDevice, Is.Not.Null);
            Assert.That(createdDevice!.Active, Is.True);
            Assert.That(updatedDevice!.Active, Is.False);
        }

        [Test]
        public async Task GetIdOfLatestEntry_ShouldReturnLatestId() {
            var device = new Device { Id = "20250829-001", Name = "Bedroom Light", Type = "Light", IpAddress = "192.168.0.5", Active = true };

            var createdDevice = await _repository.CreateDevice(device);
            var latestId = await _repository.GetIdOfLatestEntry();

            Assert.That(latestId, Is.EqualTo(createdDevice!.Id));
        }

        [Test]
        public async Task GetIdOfLatestEntry_ShouldReturnNull_WhenDevicesTableIsEmpty() {
            using (var command = _database.Connection.CreateCommand()) {
                command.CommandText = "DELETE FROM Devices";
                command.ExecuteNonQuery();
            }

            var latestId = await _repository.GetIdOfLatestEntry();

            Assert.That(latestId, Is.Null);
        }
    }
}
