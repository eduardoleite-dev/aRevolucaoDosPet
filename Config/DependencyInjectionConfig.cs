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
        services.AddScoped<IVaccineService, VaccineService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<IWeightLogService, WeightLogService>();
        services.AddScoped<IDiaryEntryService, DiaryEntryService>();
        services.AddHostedService<MongoIndexInitializer>();
        return services;
    }
}