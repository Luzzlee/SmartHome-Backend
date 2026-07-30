using Microsoft.Extensions.DependencyInjection;

namespace SmartHome.Slices.Auth.Services {
    public static class Extensions {
        public static IServiceCollection AddAuthServices(this IServiceCollection services) {
            services.AddScoped<IAuthService, AuthService>();
            return services;
        }
    }
}
