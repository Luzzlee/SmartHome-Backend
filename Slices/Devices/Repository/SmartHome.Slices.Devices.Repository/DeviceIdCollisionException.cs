namespace SmartHome.Slices.Devices.Repository {
    /// <summary>
    /// Thrown by <see cref="IDevicesRepository.CreateDevice"/> when the device's <c>Id</c> is
    /// already taken (e.g. a concurrent CreateDevice call inserted the same id in the meantime).
    /// Translates the database-specific constraint violation into a repository-level outcome so
    /// callers don't need to know which database provider or error code is involved.
    /// </summary>
    public class DeviceIdCollisionException : Exception {
        public DeviceIdCollisionException(string message, Exception? innerException = null)
            : base(message, innerException) {
        }
    }
}
