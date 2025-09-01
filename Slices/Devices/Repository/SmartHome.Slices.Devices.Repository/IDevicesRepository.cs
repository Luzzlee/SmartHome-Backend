using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Repository {
    public interface IDevicesRepository {
        Task<List<Device>> GetAllDevices();
        Task<Device?> GetDeviceById(string id);
        Task<Device?> SearchDeviceByName(string name);
        Task<Device?> CreateDevice(Device device);
        Task<Device?> SetDeviceActiveStatus(string id, bool active);
        Task<string?> GetIdOfLatestEntry();
    }
}
