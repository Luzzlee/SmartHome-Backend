using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Repository {
    public interface IDevicesRepository {
        Task<List<Device>> GetAllDevices();
        Task<Device?> GetDeviceById(string id);
        Task<Device?> SearchDeviceByName(string name);

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
        Task<string?> GetIdOfLatestEntry();
    }
}
