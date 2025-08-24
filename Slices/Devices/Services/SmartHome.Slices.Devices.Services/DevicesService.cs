using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;

namespace SmartHome.Slices.Devices.Services {
    public class DevicesService : IDevicesService {
        private readonly IDevicesRepository _repository;
        
        public DevicesService(IDevicesRepository repository) {
            _repository = repository;
        }
        
        public async Task<List<Device>> GetAllDevices() {
            var devices = await _repository.GetAllDevices();
            return devices;
        }

        public async Task<Device> GetDeviceById(int id) {
            var device = await _repository.GetDeviceById(id);
            return device;
        }

        public async Task<Device> SearchDeviceByName(string name) {
            var device = await _repository.SearchDeviceByName(name);
            return device;
        }
    }
}
