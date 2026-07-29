using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using SmartHome.Slices.Devices.Services;
using SmartHome.Api.ErrorHandling;


var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddSignalR();

builder.Services.AddSingleton<MqttHelper>();

builder.Services.AddSingleton<DbConnectionFactory>();

builder.Services.AddDeviceRepository();
builder.Services.AddDeviceServices();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
    options.AddPolicy("AllowAll", policy => {
        policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowAll");

app.UseExceptionHandler(options => { });

app.MapHub<DeviceHub>("/devicehub");

using(var scope = app.Services.CreateScope()) {
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    var mqttHelper = scope.ServiceProvider.GetRequiredService<MqttHelper>();
    await DatabaseInitializer.Initialize(dbFactory);
    await mqttHelper.ConnectAsync();
}

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();

app.MapControllers();

app.Run();