using Dapper;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SmartHome.Shared {
    public static class DatabaseInitializer {
        public static async Task Initialize(DbConnectionFactory factory) {
            using var connection = factory.CreateConnection();

            var sql = await ReadCreateScriptAsync();

            await connection.ExecuteAsync(sql);
        }

        // Read from an embedded resource rather than a path relative to the process working
        // directory - the working directory only happens to be Api/SmartHome.Api when the app is
        // started via `dotnet run --project Api/SmartHome.Api`, and isn't guaranteed in other
        // contexts (e.g. a container's WORKDIR).
        private static async Task<string> ReadCreateScriptAsync() {
            var assembly = typeof(DatabaseInitializer).Assembly;
            var resourceName = assembly.GetManifestResourceNames()
                .SingleOrDefault(name => name.EndsWith("DatabaseScripts.create.sql"))
                ?? throw new FileNotFoundException(
                    "Embedded resource 'DatabaseScripts.create.sql' not found in SmartHome.Shared.");

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
