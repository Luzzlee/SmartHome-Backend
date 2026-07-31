
using SmartHome.Shared;

namespace SmartHome.Slices.Devices.Services {
    public interface IDevicesService {
        Task<List<Device>> GetAllDevices();
        Task<Device?> GetDeviceById(string id);
        Task<List<Device>> SearchDeviceByName(string search);
        Task<Device?> CreateDevice(Device device);
        Task<Device?> SetDeviceActiveStatus(string id, bool active);

        /// <summary>Deletes the device with <paramref name="id"/>. Returns whether it was found and deleted.</summary>
        Task<bool> DeleteDevice(string id);
        Task SubscribeToDevice(string id);
        Task SwitchLight(string id, bool turnOn);
    }
}
