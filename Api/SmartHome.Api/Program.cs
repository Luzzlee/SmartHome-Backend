using SmartHome.Shared;


var builder = WebApplication.CreateBuilder(args);

var connectionString = "Data Source=SmartHome.db";

builder.Services.AddSingleton(new DbConnectionFactory(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    await DatabaseInitializer.Initialize(dbFactory);
}


app.MapControllers();

app.Run();