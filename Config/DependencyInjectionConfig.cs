using aRevolucaoDosPet.Services;
using aRevolucaoDosPet.Services.Interfaces;

namespace aRevolucaoDosPet.Config;

public static class DependencyInjectionConfig
{
    public static IServiceCollection AddDependencyInjectionConfig(this IServiceCollection services)
    {
        services.AddScoped<IMongoTestService, MongoTestService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserSettingsService, UserSettingsService>();
        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IPetFollowerService, PetFollowerService>();
        services.AddScoped<IPostService, PostService>();
        services.AddScoped<IPostReactionService, PostReactionService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IStoryService, StoryService>();
        services.AddHostedService<MongoIndexInitializer>();
        return services;
    }
}