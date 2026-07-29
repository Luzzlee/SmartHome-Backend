using MQTTnet;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace SmartHome.Shared {
    public class MqttHelper {
        private readonly IMqttClient _client;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MqttHelper> _logger;


        public MqttHelper(IHubContext<DeviceHub> hubContext, IConfiguration configuration, ILogger<MqttHelper> logger) {
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
            _client = new MqttClientFactory().CreateMqttClient();
        }

        public async Task ConnectAsync() {
            var mqttSection = _configuration.GetSection("Mqtt");
            var host = mqttSection["Host"];
            var port = int.Parse(mqttSection["Port"] ?? "1883");
            var options = new MqttClientOptionsBuilder()
               .WithTcpServer(host, port)
               .WithCredentials(mqttSection["Username"], mqttSection["Password"])
               .WithClientId(mqttSection["ClientId"])
               .WithCleanSession()
               .Build();

            _client.DisconnectedAsync += e => {
                _logger.LogWarning("MQTT client disconnected from {Host}:{Port}. Reason: {Reason}", host, port, e.Reason);
                return Task.CompletedTask;
            };

            try {
                await _client.ConnectAsync(options);
                _logger.LogInformation("MQTT client connected to {Host}:{Port} as '{ClientId}'.", host, port, mqttSection["ClientId"]);
            } catch (Exception ex) {
                _logger.LogError(ex, "MQTT client failed to connect to {Host}:{Port}.", host, port);
                throw;
            }

            _client.ApplicationMessageReceivedAsync += async e => {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

                _logger.LogDebug("Received MQTT message on topic '{Topic}' with payload '{Payload}'.", topic, payload);

                await _hubContext.Clients.All.SendAsync("DeviceMessage", new { Topic = topic, Message = payload });
            };
        }

        public async Task PublishAsync(string topic, string payload) {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .Build();

            _logger.LogDebug("Publishing MQTT message to topic '{Topic}' with payload '{Payload}'.", topic, payload);

            try {
                await _client.PublishAsync(message);
                _logger.LogInformation("Published MQTT message to topic '{Topic}'.", topic);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to publish MQTT message to topic '{Topic}'.", topic);
                throw;
            }
        }

        public async Task SubscribeAsync(string topic) {
            try {
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
                _logger.LogInformation("Subscribed to MQTT topic '{Topic}'.", topic);
            } catch (Exception ex) {
                _logger.LogError(ex, "Failed to subscribe to MQTT topic '{Topic}'.", topic);
                throw;
            }
        }
    }
}
