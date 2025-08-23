using System.Data;
using Microsoft.Data.Sqlite;

namespace SmartHome.Shared {
    public class DbConnectionFactory {
        private readonly string _connectionString;

        public DbConnectionFactory(string connectionString) {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection() {
            return new SqliteConnection(_connectionString);
        }
    }
}
