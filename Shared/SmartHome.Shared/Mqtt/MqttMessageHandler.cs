using Microsoft.AspNetCore.SignalR;

namespace SmartHome.Shared {
    public class MqttMessageHandler {
        private readonly IHubContext<DeviceHub> _hubContext;

        public MqttMessageHandler(IHubContext<DeviceHub> hubContext) {
            _hubContext = hubContext;
        }

        public async Task OnMessageReceived(string topic, string payload) {
            await _hubContext.Clients.All.SendAsync("DeviceMessage", new { Topic = topic, Message = payload });
        }
    }
}
