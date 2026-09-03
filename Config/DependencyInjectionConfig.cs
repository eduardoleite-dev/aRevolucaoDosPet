using ArquiteturaBaseApi.Services;
using ArquiteturaBaseApi.Services.Interfaces;

namespace ArquiteturaBaseApi.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddDependencyInjectionConfig(this IServiceCollection services)
    {
        services.AddScoped<IMongoTestService, MongoTestService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}