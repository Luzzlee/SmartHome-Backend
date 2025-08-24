using SmartHome.Shared;
using Dapper;

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

        public async Task<Device> GetDeviceById(int id) {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                SELECT * FROM Devices
                WHERE Id = @Id
            ";

            var device = await connection.QueryAsync<Device>(sql, new { Id = id })
                .ContinueWith(task => task.Result.FirstOrDefault());

            return device;
        }

        public async Task<Device> SearchDeviceByName(string name) {
            using var connection = _dbFactory.CreateConnection();

            var sql = @"
                SELECT * FROM Devices
                WHERE Name LIKE @Name
            ";

            var device = await connection.QueryAsync<Device>(sql, new { Name = $"%{name}%" })
                .ContinueWith(task => task.Result.FirstOrDefault());

            return device;
        }
    }
}
