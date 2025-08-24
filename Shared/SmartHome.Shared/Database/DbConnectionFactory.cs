using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace SmartHome.Shared {
    public class DbConnectionFactory {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration) {
            _connectionString = configuration.GetConnectionString("Default")!;
        }

        public IDbConnection CreateConnection() {
            return new SqliteConnection(_connectionString);
        }
    }
}
