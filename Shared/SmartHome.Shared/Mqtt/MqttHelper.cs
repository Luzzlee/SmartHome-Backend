using MQTTnet;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Buffers;

namespace SmartHome.Shared {
    public class MqttHelper {
        // Exponential backoff for the background reconnect loop, capped at MaxReconnectDelay so a
        // long-gone broker doesn't leave the app waiting minutes between attempts once it's back.
        private static readonly TimeSpan InitialReconnectDelay = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromMinutes(1);

        private readonly IMqttClient _client;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MqttHelper> _logger;

        // Guards against more than one reconnect loop running at once - a failed initial ConnectAsync()
        // and a later DisconnectedAsync event can both try to start one.
        private readonly SemaphoreSlim _reconnectGate = new(1, 1);

        private MqttClientOptions? _options;
        private string? _host;
        private int _port;

        public MqttHelper(IHubContext<DeviceHub> hubContext, IConfiguration configuration, ILogger<MqttHelper> logger) {
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
            _client = new MqttClientFactory().CreateMqttClient();
        }

        /// <summary>Whether the MQTT client currently holds an open connection to the broker. Used by the /health endpoint.</summary>
        public bool IsConnected => _client.IsConnected;

        public async Task ConnectAsync() {
            var mqttSection = _configuration.GetSection("Mqtt");
            _host = mqttSection["Host"];
            _port = int.Parse(mqttSection["Port"] ?? "1883");
            _options = new MqttClientOptionsBuilder()
               .WithTcpServer(_host, _port)
               .WithCredentials(mqttSection["Username"], mqttSection["Password"])
               .WithClientId(mqttSection["ClientId"])
               .WithCleanSession()
               .Build();

            _client.DisconnectedAsync += e => {
                _logger.LogWarning("MQTT client disconnected from {Host}:{Port}. Reason: {Reason}", _host, _port, e.Reason);
                // Only chase a reconnect if we actually lost an established connection - a failed
                // ConnectAsync() below handles its own retry and doesn't raise this event.
                if (e.ClientWasConnected) {
                    _ = ReconnectWithBackoffAsync();
                }
                return Task.CompletedTask;
            };

            _client.ApplicationMessageReceivedAsync += async e => {
                var topic = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());

                _logger.LogDebug("Received MQTT message on topic '{Topic}' with payload '{Payload}'.", topic, payload);

                await _hubContext.Clients.All.SendAsync("DeviceMessage", new { Topic = topic, Message = payload });
            };

            try {
                await _client.ConnectAsync(_options);
                _logger.LogInformation("MQTT client connected to {Host}:{Port} as '{ClientId}'.", _host, _port, mqttSection["ClientId"]);
            } catch (Exception ex) {
                _logger.LogError(ex, "MQTT client failed to connect to {Host}:{Port}. Will keep retrying in the background.", _host, _port);
                _ = ReconnectWithBackoffAsync();
                throw;
            }
        }

        /// <summary>
        /// Retries connecting to the broker with exponential backoff until it succeeds. Safe to call
        /// concurrently - only one retry loop actually runs at a time.
        /// </summary>
        private async Task ReconnectWithBackoffAsync() {
            if (_options == null || !await _reconnectGate.WaitAsync(0)) {
                return;
            }

            try {
                var delay = InitialReconnectDelay;
                while (!_client.IsConnected) {
                    await Task.Delay(delay);

                    try {
                        await _client.ConnectAsync(_options);
                        _logger.LogInformation("MQTT client reconnected to {Host}:{Port}.", _host, _port);
                    } catch (Exception ex) {
                        _logger.LogWarning(ex, "MQTT reconnect attempt to {Host}:{Port} failed. Retrying in {DelaySeconds}s.", _host, _port, delay.TotalSeconds);
                        delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, MaxReconnectDelay.Ticks));
                    }
                }
            } finally {
                _reconnectGate.Release();
            }
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
