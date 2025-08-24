
using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Services {
    public interface IDevicesService {
        Task<List<Device>> GetAllDevices();
        Task<Device> GetDeviceById(int id);
        Task<Device> SearchDeviceByName(string search);
    }
}
