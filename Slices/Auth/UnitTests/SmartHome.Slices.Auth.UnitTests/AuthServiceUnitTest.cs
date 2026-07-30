using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SmartHome.Slices.Auth.Services;

namespace SmartHome.Slices.Auth.UnitTests {

    [TestFixture]
    public class AuthServiceUnitTest {
        private const string ConfiguredUsername = "admin";
        private const string ConfiguredPassword = "correct-password";

        private AuthService BuildService(string? username, string? passwordHash) {
            var settings = new Dictionary<string, string?> {
                ["Auth:Username"] = username,
                ["Auth:PasswordHash"] = passwordHash
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();

            return new AuthService(configuration);
        }

        private static string HashPassword(string password) {
            return new PasswordHasher<string>().HashPassword(ConfiguredUsername, password);
        }

        [Test]
        public void ValidateCredentials_WithCorrectUsernameAndPassword_ReturnsTrue() {
            var service = BuildService(ConfiguredUsername, HashPassword(ConfiguredPassword));

            var result = service.ValidateCredentials(ConfiguredUsername, ConfiguredPassword);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ValidateCredentials_WithWrongPassword_ReturnsFalse() {
            var service = BuildService(ConfiguredUsername, HashPassword(ConfiguredPassword));

            var result = service.ValidateCredentials(ConfiguredUsername, "wrong-password");

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCredentials_WithWrongUsername_ReturnsFalse() {
            var service = BuildService(ConfiguredUsername, HashPassword(ConfiguredPassword));

            var result = service.ValidateCredentials("someone-else", ConfiguredPassword);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCredentials_WhenAuthNotConfigured_ReturnsFalse() {
            var service = BuildService(username: null, passwordHash: null);

            var result = service.ValidateCredentials(ConfiguredUsername, ConfiguredPassword);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ValidateCredentials_WithMalformedPasswordHash_ReturnsFalse() {
            // e.g. the placeholder value still present in appsettings.Development.json.
            var service = BuildService(ConfiguredUsername, "CHANGE_ME_SET_VIA_USER_SECRETS_OR_ENV_VAR");

            var result = service.ValidateCredentials(ConfiguredUsername, ConfiguredPassword);

            Assert.That(result, Is.False);
        }
    }
}
