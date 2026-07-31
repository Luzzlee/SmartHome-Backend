using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Repository {
    public interface IDevicesRepository {
        Task<List<Device>> GetAllDevices();
        Task<Device?> GetDeviceById(string id);

        /// <summary>
        /// Returns every device whose name contains <paramref name="name"/> as a substring
        /// (case-sensitivity depends on the underlying SQLite collation). Wildcard-wrapping for the
        /// SQL LIKE query happens here - callers pass a plain substring, not LIKE syntax.
        /// </summary>
        Task<List<Device>> SearchDeviceByName(string name);

        /// <summary>
        /// Inserts <paramref name="device"/> and returns the persisted entity.
        /// </summary>
        /// <exception cref="DeviceIdCollisionException">
        /// Thrown when <paramref name="device"/>'s Id is already taken (e.g. a concurrent
        /// CreateDevice call inserted the same id in the meantime). Callers should generate a
        /// new id and retry.
        /// </exception>
        Task<Device?> CreateDevice(Device device);
        Task<Device?> SetDeviceActiveStatus(string id, bool active);

        /// <summary>Deletes the device with <paramref name="id"/>. Returns whether a row was deleted.</summary>
        Task<bool> DeleteDevice(string id);
        Task<string?> GetIdOfLatestEntry();
    }
}
