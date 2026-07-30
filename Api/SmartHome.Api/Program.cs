using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using SmartHome.Slices.Devices.Services;
using SmartHome.Slices.Auth.Services;
using SmartHome.Api.ErrorHandling;
using Microsoft.AspNetCore.Authentication.Cookies;


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

builder.Services.AddAuthServices();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowFrontend", policy => {
        policy.WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

// Cookie-based session auth for the single, statically configured user (see "Auth" config section).
// Requests without a valid cookie get a 401 instead of the default redirect-to-login-page behavior,
// since this is an API, not a page-rendering app.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.Cookie.Name = "SmartHome.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Events.OnRedirectToLogin = context => {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context => {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowFrontend");

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
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();