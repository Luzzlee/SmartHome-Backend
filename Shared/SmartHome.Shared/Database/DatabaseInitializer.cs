using Dapper;
using System.IO;

namespace SmartHome.Shared {
    public static class DatabaseInitializer {
        public static async Task Initialize(DbConnectionFactory factory) {
            using var connection = factory.CreateConnection();

            var sql = File.ReadAllText("../../Shared/SmartHome.Shared/DatabaseScripts/create.sql");

            await connection.ExecuteAsync(sql);
        }
    }
}
