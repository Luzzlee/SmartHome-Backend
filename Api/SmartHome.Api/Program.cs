using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using SmartHome.Slices.Devices.Services;
using SmartHome.Slices.Auth.Services;
using SmartHome.Api.Auth;
using SmartHome.Api.ErrorHandling;
using SmartHome.Api.HealthChecks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;


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

// Server-side session store: without this, the cookie itself is the *only* record of a session, so
// logout (which just clears the browser's cookie) can't actually revoke an already-issued cookie - a
// replayed old cookie value stays valid until its own expiry. Wiring an ITicketStore in makes every
// authenticated request check the in-memory store, not just the cookie's own encrypted validity, and
// makes logout remove the session from that store so a replayed cookie is rejected immediately.
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, ticketStore) => {
        options.SessionStore = ticketStore;
    });

builder.Services.AddAuthorization();

// GET /health reports DB and MQTT connectivity separately (see HealthChecks/) so it can be used as a
// simple readiness/liveness probe without needing an authenticated session.
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<MqttHealthCheck>("mqtt");

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors("AllowFrontend");

app.UseExceptionHandler(options => { });

app.MapHub<DeviceHub>("/devicehub").RequireAuthorization();

// Anonymous on purpose: external monitoring/load balancers need to reach this without a session cookie.
app.MapHealthChecks("/health", new HealthCheckOptions {
    ResponseWriter = HealthCheckResponseWriter.WriteResponse
}).AllowAnonymous();

using(var scope = app.Services.CreateScope()) {
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    var mqttHelper = scope.ServiceProvider.GetRequiredService<MqttHelper>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    await DatabaseInitializer.Initialize(dbFactory);

    try {
        await mqttHelper.ConnectAsync();
    } catch (Exception) {
        // MqttHelper already logged the root cause and kicked off a background reconnect loop - a
        // broker that's unreachable at startup shouldn't prevent the DB and non-MQTT endpoints from
        // starting up.
        logger.LogWarning("Starting the app without an active MQTT connection; will keep retrying in the background.");
    }
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