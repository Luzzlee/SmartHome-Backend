namespace SmartHome.Slices.Auth.Services {
    public interface IAuthService {
        /// <summary>
        /// Validates the given credentials against the single, statically configured user
        /// (see "Auth" configuration section). Returns the configured username on success.
        /// </summary>
        bool ValidateCredentials(string username, string password);

        /// <summary>
        /// Username of the single, statically configured user.
        /// </summary>
        string? ConfiguredUsername { get; }
    }
}
