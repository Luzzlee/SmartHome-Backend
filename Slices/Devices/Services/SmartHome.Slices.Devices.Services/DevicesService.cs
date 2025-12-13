using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SmartHome.Slices.Devices.Services {
    public class DevicesService : IDevicesService {
        private readonly IDevicesRepository _repository;
        private readonly MqttHelper _mqttHelper;

        public DevicesService(IDevicesRepository repository, MqttHelper mqttHelper) {
            _repository = repository;
            _mqttHelper = mqttHelper;
        }
        
        public async Task<List<Device>> GetAllDevices() {
            var result = await _repository.GetAllDevices();
            return result;
        }

        public async Task<Device?> GetDeviceById(string id) {
            ValidateId(id);
            var result = await _repository.GetDeviceById(id);
            return result;
        }

        public async Task<Device?> SearchDeviceByName(string name) {
            ValidateName(name);
            var result = await _repository.SearchDeviceByName(name);
            return result;
        }

        public async Task<Device?> CreateDevice(Device device) {
            ValidateDevice(device);
            device.Id = await GenerateNewId();

            var result = await _repository.CreateDevice(device);
            return result;
        }

        public async Task<Device?> SetDeviceActiveStatus(string id, bool active) {
            ValidateId(id);
            var result = await _repository.SetDeviceActiveStatus(id, active);
            return result;
        }

        public async Task SubscribeToDevice(string id) {
            var device = await GetDeviceById(id);
            if(device == null) {
                throw new ArgumentException($"Device with id '{id}' not found.", nameof(id));
            }
            string topic = $"smarthome/{device.Type.ToLower()}/{device.Name.ToLower()}/{device.Id}";
            await _mqttHelper.SubscribeAsync(topic);
        }

        public async Task SwitchLight(string id, bool turnOn) {
            var device = await GetDeviceById(id);
            if(device == null) {
                throw new ArgumentException($"Device with id '{id}' not found.", nameof(id));
            }
            if(device.Type.ToLower() != "light") {
                throw new ArgumentException($"Device with id '{id}' is not a light.", nameof(id));
            }
            string topic = $"smarthome/{device.Type.ToLower()}/{device.Name.ToLower()}/{device.Id}";
            var payload = turnOn ? "ON" : "OFF";

            await _mqttHelper.PublishAsync(topic, payload);
        }

        private async Task<string> GenerateNewId() {
            var latestId = await _repository.GetIdOfLatestEntry();
            var datePart = latestId!.Split('-')[0];
            var counterPart = latestId!.Split('-')[1];
                        
            if (datePart != DateTime.UtcNow.ToString("yyyyMMdd")) {
                return DateTime.UtcNow.ToString("yyyyMMdd") + "-001";
            }

            int counter = 1;
            if (int.TryParse(counterPart, out int newCounter)) {
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

            if (!ValidateIPv4(device.IpAddress)) {
                throw new ArgumentException("Device IP address is not valid.", nameof(device.IpAddress));
            }
        }

        private void ValidateId(string id) {
            if(string.IsNullOrEmpty(id)) {
                throw new ArgumentException("Device ID cannot be empty.", nameof(id));
            }

            if (!Regex.IsMatch(id, @"^\d{8}-\d{3}$")) {
                throw new ArgumentException($"Device ID '{id}' does not match pattern. Expected Format: yyyyMMdd-XXX (z. B. 20250829-001)", nameof(id));
            }

            var datePart = id.Split('-').FirstOrDefault();
            if(!DateTime.TryParseExact(datePart, "yyyyMMdd", null, DateTimeStyles.None, out _)) {
                throw new ArgumentException($"Invalid date '{datePart}' in id '{id}'. Expected Format: yyyyMMdd-XXX (z. B. 20250829-001)", nameof(id));
            }
        }

        private void ValidateName(string name) {
            if(string.IsNullOrEmpty(name)) {
                throw new ArgumentException("Device name cannot be empty.", nameof(name));
            }
        }

        private bool ValidateIPv4(string address) {
            if (string.IsNullOrEmpty(address)) {
                return false;
            }

            var splitValues = address.Split('.');
            if(splitValues.Length != 4) {
                return false;
            }

            return splitValues.All(r => byte.TryParse(r, out _));
        }
    }
}
