using SmartHome.Shared;
using Dapper;
using Microsoft.Data.Sqlite;

namespace SmartHome.Slices.Devices.Repository {
    public class DevicesRepository : IDevicesRepository {
       private readonly DbConnectionFactory _dbFactory;

        public DevicesRepository(DbConnectionFactory dbFactory) {
            _dbFactory = dbFactory;
        }

        public async Task<List<Device>> GetAllDevices() {
            using var connection = _dbFactory.CreateConnection();

            var sql = "SELECT * FROM Devices";

            var devices = await connection.QueryAsync<Device>(sql)
                .ContinueWith(task => task.Result.ToList());

            return devices;
        }

        public async Task<Device?> GetDeviceById(string id) {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                SELECT * FROM Devices
                WHERE Id = @Id
            ";

            var device = await connection.QueryAsync<Device>(sql, new { Id = $"{id}" })
                .ContinueWith(task => task.Result.FirstOrDefault());

            return device;
        }

        public async Task<Device?> SearchDeviceByName(string name) {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                SELECT * FROM Devices
                WHERE Name LIKE @Name
            ";

            var device = await connection.QueryAsync<Device>(sql, new { Name = $"{name}" })
                .ContinueWith(task => task.Result.FirstOrDefault());

            return device;
        }

        public async Task<Device?> CreateDevice(Device device) {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                INSERT INTO Devices (Id, Name, Type, IpAddress, Active)
                VALUES (@Id, @Name, @Type, @IpAddress, @Active)
            ";

            try {
                await connection.ExecuteAsync(sql, device);
            } catch (SqliteException ex) when (IsDeviceIdCollision(ex)) {
                throw new DeviceIdCollisionException($"Device id '{device.Id}' is already taken.", ex);
            }

            var createdDevice = await GetDeviceById(device.Id);

            return createdDevice;
        }

        private static bool IsDeviceIdCollision(SqliteException ex) {
            return ex.SqliteErrorCode == 19 // SQLITE_CONSTRAINT
                && ex.Message.Contains("Devices.Id", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<Device?> SetDeviceActiveStatus(string id, bool active) {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                UPDATE Devices
                SET Active = @Active
                WHERE Id = @Id
            ";

            await connection.ExecuteAsync(sql, new { Id = id, Active = active });

           var device = await GetDeviceById(id);

            return device;
        }

        public async Task<string?> GetIdOfLatestEntry() {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                SELECT Id FROM Devices 
                ORDER BY Id DESC
                LIMIT 1
            ";

            var latestId = await connection.QueryFirstOrDefaultAsync<string>(sql);

            return latestId;
        }
    }
}
