using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace ArquiteturaBaseApi.Config;

public static class MongoConfig
{
    public static IServiceCollection AddMongoConfig(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["ConnectionStrings__MongoDB"] 
                               ?? configuration["ConnectionStrings:MongoDB"] 
                               ?? configuration["MongoDbSettings:ConnectionString"] 
                               ?? throw new InvalidOperationException("String de conexão do MongoDB não encontrada.");

        var mongoUrl = MongoUrl.Create(connectionString);
        var mongoClient = new MongoClient(mongoUrl);
        var database = mongoClient.GetDatabase(mongoUrl.DatabaseName ?? "PortfolioDb");

        services.AddSingleton<IMongoClient>(mongoClient);
        services.AddSingleton<IMongoDatabase>(database);

        return services;
    }
}