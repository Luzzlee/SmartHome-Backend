using Dapper;

namespace SmartHome.Shared {
    public static class DatabaseInitializer {
        public static async Task Initialize(DbConnectionFactory factory) {
            using var connection = factory.CreateConnection();

            var sql = @"
            CREATE TABLE IF NOT EXISTS Devices (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Type TEXT NOT NULL,
                IpAddress TEXT NOT NULL
                Active BOOLEAN NOT NULL CHECK (Active IN (0, 1))
            );
        ";

            await connection.ExecuteAsync(sql);
        }
    }
}
