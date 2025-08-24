using Microsoft.Extensions.DependencyInjection;

namespace SmartHome.Slices.Devices.Repository {
    public static class Extensions {
        public static IServiceCollection AddDeviceRepository(this IServiceCollection services) {
            services.AddScoped<IDevicesRepository, DevicesRepository>();
            return services;
        }
    }
}