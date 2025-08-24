using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Repository {
    public interface IDevicesRepository {
        Task<List<Device>> GetAllDevices();
        Task<Device> GetDeviceById(int id);
        Task<Device> SearchDeviceByName(string name);
    }
}
