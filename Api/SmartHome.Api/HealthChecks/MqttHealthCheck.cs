using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartHome.Shared;

namespace SmartHome.Api.HealthChecks {

    /// <summary>Reports whether the MQTT client currently holds an open connection to the broker.</summary>
    public class MqttHealthCheck : IHealthCheck {
        private readonly MqttHelper _mqttHelper;

        public MqttHealthCheck(MqttHelper mqttHelper) {
            _mqttHelper = mqttHelper;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
            var result = _mqttHelper.IsConnected
                ? HealthCheckResult.Healthy("MQTT client is connected.")
                : HealthCheckResult.Unhealthy("MQTT client is not connected.");

            return Task.FromResult(result);
        }
    }
}
