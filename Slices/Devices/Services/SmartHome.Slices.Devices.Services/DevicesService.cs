using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;

namespace SmartHome.Slices.Devices.Services {
    public class DevicesService : IDevicesService {
        private readonly IDevicesRepository _repository;
        
        public DevicesService(IDevicesRepository repository) {
            _repository = repository;
        }
        
        public async Task<List<Device>> GetAllDevices() {
            var result = await _repository.GetAllDevices();
            return result;
        }

        public async Task<Device> GetDeviceById(string id) {
            var result = await _repository.GetDeviceById(id);
            return result;
        }

        public async Task<Device> SearchDeviceByName(string name) {
            var result = await _repository.SearchDeviceByName(name);
            return result;
        }

        public async Task<Device> CreateDevice(Device device) {
            ValidateDevice(device);
            device.Id = await GenerateNewId();

            var result = await _repository.CreateDevice(device);
            return result;
        }

        private async Task<string> GenerateNewId() {
            var latestId = await _repository.GetIdOfLatestEntry();
            
            if (string.IsNullOrEmpty(latestId)) {
                return DateTime.UtcNow.ToString("yyyyMMdd") + "-001";
            }

            int counter = 1;
            var todayCounter = latestId.Split('-').Last();
            if (int.TryParse(todayCounter, out int newCounter)) {
                counter = newCounter + 1;
            }
            
            if (counter != 1) {
                string id = DateTime.UtcNow.ToString("yyyyMMdd") + "-" + counter.ToString("D3");
                return id;
            }

            throw new Exception("Id could not be generated.");
        }

        private void ValidateDevice(Device device) {
            if(string.IsNullOrEmpty(device.Name)) {
                throw new ArgumentException("Device name cannot be empty.", nameof(device.Name));
            }
            
            if(string.IsNullOrEmpty(device.Type)) {
                throw new ArgumentException("Device type cannot be empty.", nameof(device.Type));
            }
            
            if(string.IsNullOrEmpty(device.IpAddress)) {
                throw new ArgumentException("Device IP address cannot be empty.", nameof(device.IpAddress));
            }

            if (!System.Net.IPAddress.TryParse(device.IpAddress, out _)) {
                throw new ArgumentException("Device IP address is not valid.", nameof(device.IpAddress));
            }
        }
    }
}
