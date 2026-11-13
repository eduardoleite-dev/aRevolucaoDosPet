using aRevolucaoDosPet.Services;
using aRevolucaoDosPet.Services.Interfaces;
using CloudinaryDotNet;

namespace aRevolucaoDosPet.Config;

public static class CloudinaryConfig
{
    public static IServiceCollection AddCloudinaryConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var cloudName = configuration["CloudinarySettings:CloudName"];
        var apiKey = configuration["CloudinarySettings:ApiKey"];
        var apiSecret = configuration["CloudinarySettings:ApiSecret"];

        if (string.IsNullOrWhiteSpace(cloudName) || string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(apiSecret))
            throw new InvalidOperationException("Configure CloudinarySettings:CloudName, CloudinarySettings:ApiKey e CloudinarySettings:ApiSecret.");

        services.AddSingleton(new Cloudinary(new Account(cloudName, apiKey, apiSecret)));
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        return services;
    }
}