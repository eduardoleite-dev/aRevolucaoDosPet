using aRevolucaoDosPet.Services;
using aRevolucaoDosPet.Services.Interfaces;

namespace aRevolucaoDosPet.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddDependencyInjectionConfig(this IServiceCollection services)
    {
        services.AddScoped<IMongoTestService, MongoTestService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}