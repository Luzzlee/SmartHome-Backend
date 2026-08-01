using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartHome.Api.Auth;

namespace SmartHome.Api.UnitTests {

    /// <summary>
    /// Exercises the real ASP.NET Core cookie-authentication pipeline, wired up the same way as
    /// Program.cs (AddCookie + InMemoryTicketStore as SessionStore), against a minimal test host.
    /// AuthControllerUnitTest mocks IAuthenticationService, so it can't prove anything about whether a
    /// cookie is actually still accepted after logout - that behavior lives entirely in the cookie
    /// handler + session store, not in AuthController itself. This test proves the actual mechanism:
    /// a cookie captured before logout is rejected (401) once replayed after logout.
    /// </summary>
    [TestFixture]
    public class SessionInvalidationTest {
        private IHost _host = null!;
        private TestServer _server = null!;

        [SetUp]
        public async Task Setup() {
            _host = await new HostBuilder()
                .ConfigureWebHost(webBuilder => {
                    webBuilder.UseTestServer();

                    webBuilder.ConfigureServices(services => {
                        services.AddRouting();
                        services.AddAuthorization();
                        services.AddMemoryCache();
                        services.AddSingleton<ITicketStore, InMemoryTicketStore>();

                        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                            .AddCookie(options => {
                                options.Events.OnRedirectToLogin = context => {
                                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                    return Task.CompletedTask;
                                };
                            });

                        // Same wiring as Program.cs: the cookie handler's SessionStore is resolved from DI
                        // instead of being newed up inline, so sign-in/sign-out go through the shared store.
                        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
                            .Configure<ITicketStore>((options, ticketStore) => {
                                options.SessionStore = ticketStore;
                            });
                    });

                    webBuilder.Configure(app => {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints => {
                            endpoints.MapPost("/login", async context => {
                                var identity = new ClaimsIdentity(
                                    new[] { new Claim(ClaimTypes.Name, "admin") },
                                    CookieAuthenticationDefaults.AuthenticationScheme);

                                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
                            });

                            endpoints.MapPost("/logout", async context => {
                                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                            });

                            endpoints.MapGet("/protected", context => {
                                context.Response.StatusCode = StatusCodes.Status200OK;
                                return Task.CompletedTask;
                            }).RequireAuthorization();
                        });
                    });
                })
                .StartAsync();

            _server = _host.GetTestServer();
        }

        [TearDown]
        public void TearDown() {
            _server.Dispose();
            _host.Dispose();
        }

        private static string ExtractCookie(HttpResponseMessage response) {
            var setCookie = response.Headers.GetValues("Set-Cookie").First();
            return setCookie.Split(';')[0];
        }

        [Test]
        public async Task ProtectedEndpoint_WithFreshlyIssuedCookie_ReturnsOk() {
            var client = _server.CreateClient();

            var loginResponse = await client.PostAsync("/login", content: null);
            var cookie = ExtractCookie(loginResponse);

            var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
            request.Headers.Add("Cookie", cookie);
            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task ProtectedEndpoint_ReplayedWithCookieFromBeforeLogout_ReturnsUnauthorized() {
            var client = _server.CreateClient();

            var loginResponse = await client.PostAsync("/login", content: null);
            var cookieFromBeforeLogout = ExtractCookie(loginResponse);

            // Sanity check: the cookie is valid before logout.
            var beforeLogoutRequest = new HttpRequestMessage(HttpMethod.Get, "/protected");
            beforeLogoutRequest.Headers.Add("Cookie", cookieFromBeforeLogout);
            var beforeLogoutResponse = await client.SendAsync(beforeLogoutRequest);
            Assert.That(beforeLogoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/logout");
            logoutRequest.Headers.Add("Cookie", cookieFromBeforeLogout);
            await client.SendAsync(logoutRequest);

            // Replay the *old* cookie value captured before logout - the cookie's own encrypted contents
            // are still technically valid, but the session behind it must have been removed server-side.
            var replayRequest = new HttpRequestMessage(HttpMethod.Get, "/protected");
            replayRequest.Headers.Add("Cookie", cookieFromBeforeLogout);
            var replayResponse = await client.SendAsync(replayRequest);

            Assert.That(replayResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
