using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using SmartHome.Api.Models;

namespace SmartHome.Api.UnitTests {

    /// <summary>
    /// Boots the real app - real Program.cs, real DI container, real middleware pipeline - via
    /// WebApplicationFactory&lt;Program&gt; and drives the shipped /auth/login, /auth/logout and
    /// /auth/me endpoints end-to-end. AuthControllerUnitTest mocks IAuthenticationService, so it
    /// can't prove anything about whether a cookie is actually still accepted after logout - that
    /// behavior lives entirely in the cookie handler + session store wiring in Program.cs, not in
    /// AuthController itself. This test proves the actual mechanism against the real wiring: a
    /// cookie captured before logout is rejected (401) once replayed after logout.
    ///
    /// Auth:PasswordHash and ConnectionStrings:Default are overridden per-fixture instead of relying
    /// on machine-level user-secrets/env vars (see repo CLAUDE.md), so this runs the same on any dev
    /// machine or CI runner. The MQTT broker is left unconfigured on purpose: startup is resilient to
    /// an unreachable broker (see "MQTT connectivity and health checks" in CLAUDE.md), so the app
    /// still boots and these auth-only endpoints stay reachable without a live broker.
    /// </summary>
    [TestFixture]
    public class SessionInvalidationUnitTest {
        private const string TestUsername = "admin";
        private const string TestPassword = "correct-password";

        private WebApplicationFactory<Program> _factory = null!;
        private string _dbPath = null!;

        [OneTimeSetUp]
        public void OneTimeSetup() {
            _dbPath = Path.Combine(Path.GetTempPath(), $"smarthome-test-{Guid.NewGuid():N}.db");
            var passwordHash = new PasswordHasher<string>().HashPassword(TestUsername, TestPassword);

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => {
                    builder.UseEnvironment("Development");
                    builder.ConfigureAppConfiguration((_, config) => {
                        config.AddInMemoryCollection(new Dictionary<string, string?> {
                            ["Auth:PasswordHash"] = passwordHash,
                            ["ConnectionStrings:Default"] = $"Data Source={_dbPath};Cache=Shared;Mode=ReadWriteCreate",
                        });
                    });
                });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() {
            _factory.Dispose();

            // Microsoft.Data.Sqlite pools connections under the hood, so a plain connection.Dispose()
            // doesn't necessarily release the underlying file handle - clear the pool first or the
            // temp db file below can still be locked.
            SqliteConnection.ClearAllPools();

            if (File.Exists(_dbPath)) {
                File.Delete(_dbPath);
            }
        }

        private async Task<string> LoginAndGetCookieAsync(HttpClient client) {
            var response = await client.PostAsJsonAsync("/auth/login", new LoginRequest { Username = TestUsername, Password = TestPassword });
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            return ExtractCookie(response);
        }

        private static string ExtractCookie(HttpResponseMessage response) {
            var setCookie = response.Headers.GetValues("Set-Cookie").First();
            return setCookie.Split(';')[0];
        }

        [Test]
        public async Task Login_WithValidCredentials_ShouldAllowAccessToProtectedEndpoint() {
            var client = _factory.CreateClient();
            var cookie = await LoginAndGetCookieAsync(client);

            var request = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
            request.Headers.Add("Cookie", cookie);
            var response = await client.SendAsync(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Logout_ThenReplayOldCookie_ShouldReturnUnauthorized() {
            var client = _factory.CreateClient();
            var cookieFromBeforeLogout = await LoginAndGetCookieAsync(client);

            // Sanity check: the cookie is valid before logout.
            var beforeLogoutRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
            beforeLogoutRequest.Headers.Add("Cookie", cookieFromBeforeLogout);
            var beforeLogoutResponse = await client.SendAsync(beforeLogoutRequest);
            Assert.That(beforeLogoutResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/auth/logout");
            logoutRequest.Headers.Add("Cookie", cookieFromBeforeLogout);
            await client.SendAsync(logoutRequest);

            // Replay the *old* cookie value captured before logout - the cookie's own encrypted
            // contents are still technically valid, but the session behind it must have been removed
            // server-side.
            var replayRequest = new HttpRequestMessage(HttpMethod.Get, "/auth/me");
            replayRequest.Headers.Add("Cookie", cookieFromBeforeLogout);
            var replayResponse = await client.SendAsync(replayRequest);

            Assert.That(replayResponse.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }
    }
}
