using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using SmartHome.Slices.Devices.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddDeviceRepository();
builder.Services.AddDeviceServices();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using(var scope = app.Services.CreateScope()) {
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    await DatabaseInitializer.Initialize(dbFactory);
}

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();