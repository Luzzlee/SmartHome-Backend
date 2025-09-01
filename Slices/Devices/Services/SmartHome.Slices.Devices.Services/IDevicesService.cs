
using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Services {
    public interface IDevicesService {
        Task<List<Device>> GetAllDevices();
        Task<Device?> GetDeviceById(string id);
        Task<Device?> SearchDeviceByName(string search);
        Task<Device?> CreateDevice(Device device);
        Task<Device?> SetDeviceActiveStatus(string id, bool active);
    }
}
