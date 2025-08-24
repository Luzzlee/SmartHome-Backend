using Microsoft.Extensions.DependencyInjection;

namespace SmartHome.Slices.Devices.Services {
    public static class Extensions {
        public static IServiceCollection AddDeviceServices(this IServiceCollection services) {
            services.AddScoped<IDevicesService, DevicesService>();
            return services;
        }
    }
}
