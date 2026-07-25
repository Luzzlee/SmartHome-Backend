using MQTTnet;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System.Buffers;

namespace SmartHome.Shared {
    public class MqttHelper {
        private readonly IMqttClient _client;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly IConfiguration _configuration;


        public MqttHelper(IHubContext<DeviceHub> hubContext, IConfiguration configuration) {
            _hubContext = hubContext;
            _configuration = configuration;
            _client = new MqttClientFactory().CreateMqttClient();
        }

        public async Task ConnectAsync() {
            var mqttSection = _configuration.GetSection("Mqtt");
            var port = int.Parse(mqttSection["Port"] ?? "1883");
            var options = new MqttClientOptionsBuilder()
               .WithTcpServer(mqttSection["Host"], port)
               .WithCredentials(mqttSection["Username"], mqttSection["Password"])
               .WithClientId(mqttSection["ClientId"])
               .WithCleanSession()
               .Build();
            
            await _client.ConnectAsync(options);

            _client.ApplicationMessageReceivedAsync += async e => {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

                await _hubContext.Clients.All.SendAsync("DeviceMessage", new { Topic = topic, Message = payload });
            };
        }

        public async Task PublishAsync(string topic, string payload) {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();
             
            await _client.PublishAsync(message);
        }

        public async Task SubscribeAsync(string topic) {
            await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }
    }
}
