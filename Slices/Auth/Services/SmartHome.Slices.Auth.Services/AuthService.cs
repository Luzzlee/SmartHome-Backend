using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace SmartHome.Slices.Auth.Services {
    /// <summary>
    /// Validates login attempts against the single, statically configured user (config/user-secrets/env-var,
    /// analogous to the existing "Mqtt:Password" pattern - see "Auth" section). No user table/registration:
    /// this system is built for a single user/household.
    /// </summary>
    public class AuthService : IAuthService {
        private readonly IConfiguration _configuration;
        private readonly PasswordHasher<string> _passwordHasher = new();

        public AuthService(IConfiguration configuration) {
            _configuration = configuration;
        }

        public string? ConfiguredUsername => _configuration["Auth:Username"];

        public bool ValidateCredentials(string username, string password) {
            var configuredUsername = ConfiguredUsername;
            var configuredPasswordHash = _configuration["Auth:PasswordHash"];

            if (string.IsNullOrEmpty(configuredUsername) || string.IsNullOrEmpty(configuredPasswordHash)) {
                return false;
            }

            if (!string.Equals(username, configuredUsername, StringComparison.Ordinal)) {
                return false;
            }

            try {
                var result = _passwordHasher.VerifyHashedPassword(configuredUsername, configuredPasswordHash, password);
                return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
            } catch (FormatException) {
                // Malformed hash (e.g. still the placeholder from appsettings.Development.json) -> never a valid login.
                return false;
            }
        }
    }
}
