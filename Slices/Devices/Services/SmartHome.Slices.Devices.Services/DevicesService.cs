using SmartHome.Shared;
using SmartHome.Slices.Devices.Repository;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SmartHome.Slices.Devices.Services {
    public class DevicesService : IDevicesService {
        private readonly IDevicesRepository _repository;
        private readonly MqttHelper _mqttHelper;
        private readonly ILogger<DevicesService> _logger;

        public DevicesService(IDevicesRepository repository, MqttHelper mqttHelper, ILogger<DevicesService> logger) {
            _repository = repository;
            _mqttHelper = mqttHelper;
            _logger = logger;
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

        // Two concurrent CreateDevice calls can read the same "latest id" and thus generate the
        // same new id. Devices.Id is the table's PRIMARY KEY (see create.sql), so the repository
        // throws DeviceIdCollisionException instead of silently duplicating it. Retry a bounded
        // number of times with a freshly generated id whenever that happens.
        private const int MaxCreateDeviceAttempts = 5;

        public async Task<Device?> CreateDevice(Device device) {
            _logger.LogInformation("Creating device '{DeviceName}' ({DeviceType}).", device.Name, device.Type);
            try {
                ValidateDevice(device);

                DeviceIdCollisionException? lastCollision = null;
                for (var attempt = 1; attempt <= MaxCreateDeviceAttempts; attempt++) {
                    device.Id = await GenerateNewId();

                    try {
                        var result = await _repository.CreateDevice(device);
                        if (result != null) {
                            _logger.LogInformation("Device '{DeviceId}' ('{DeviceName}') created successfully.", result.Id, result.Name);
                        } else {
                            _logger.LogWarning("Device '{DeviceName}' could not be written to the database.", device.Name);
                        }
                        return result;
                    } catch (DeviceIdCollisionException ex) {
                        // Id was taken by a concurrent CreateDevice call in the meantime - regenerate and retry.
                        lastCollision = ex;
                    }
                }

                throw new InvalidOperationException($"Could not generate a unique device id after {MaxCreateDeviceAttempts} attempts.", lastCollision);
            } catch (Exception ex) {
                _logger.LogWarning("Failed to create device '{DeviceName}': {Message}", device.Name, ex.Message);
                throw;
            }
        }

        public async Task<Device?> SetDeviceActiveStatus(string id, bool active) {
            ValidateId(id);
            var result = await _repository.SetDeviceActiveStatus(id, active);
            return result;
        }

        public async Task SubscribeToDevice(string id) {
            _logger.LogInformation("Subscribing to device '{DeviceId}'.", id);
            try {
                var device = await GetDeviceById(id);
                if(device == null) {
                    throw new ArgumentException($"Device with id '{id}' not found.", nameof(id));
                }
                string topic = $"smarthome/{device.Type.ToLower()}/{device.Name.ToLower()}/{device.Id}";
                await _mqttHelper.SubscribeAsync(topic);
                _logger.LogInformation("Subscribed to device '{DeviceId}' on topic '{Topic}'.", id, topic);
            } catch (Exception ex) {
                _logger.LogWarning("Failed to subscribe to device '{DeviceId}': {Message}", id, ex.Message);
                throw;
            }
        }

        public async Task SwitchLight(string id, bool turnOn) {
            _logger.LogInformation("Switching light '{DeviceId}' {State}.", id, turnOn ? "ON" : "OFF");
            try {
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
                _logger.LogInformation("Light '{DeviceId}' switched {State} successfully.", id, turnOn ? "ON" : "OFF");
            } catch (Exception ex) {
                _logger.LogWarning("Failed to switch light '{DeviceId}' {State}: {Message}", id, turnOn ? "ON" : "OFF", ex.Message);
                throw;
            }
        }

        private async Task<string> GenerateNewId() {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var latestId = await _repository.GetIdOfLatestEntry();

            // Empty Devices table (e.g. very first device ever created) -> no "-001" case to check
            if (latestId == null) {
                return today + "-001";
            }

            var parts = latestId.Split('-');
            var datePart = parts[0];
            var counterPart = parts.Length > 1 ? parts[1] : null;

            if (datePart != today) {
                return today + "-001";
            }

            // Fall back to counter 1 if today's latest counterPart is missing/not a valid number
            // (e.g. data corruption) rather than throwing - "001" is always a safe id to try next.
            var counter = 1;
            if (counterPart != null && int.TryParse(counterPart, out var parsedCounter)) {
                counter = parsedCounter + 1;
            }

            return today + "-" + counter.ToString("D3");
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
