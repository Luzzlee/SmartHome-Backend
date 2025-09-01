

using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace SmartHome.Slices.Devices.UnitTests {
    public class SqliteTestDatabaseProvider : IDisposable {
        public IDbConnection Connection { get; }

        public SqliteTestDatabaseProvider() {
            Connection = new SqliteConnection("Data Source=testdb.sqlite;Mode=ReadWriteCreate;Cache=Shared");
            Connection.Open();
            
            ExecuteScript("../../../TestData/schema.sql");
            ExecuteScript("../../../TestData/insert.sql");
        }

        private void ExecuteScript(string path) {
            var sql = File.ReadAllText(path);
            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        public void Dispose() {
            Connection.Close();
            Connection.Dispose();
        }
    }
}
