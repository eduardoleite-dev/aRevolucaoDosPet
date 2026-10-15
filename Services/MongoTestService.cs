using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using aRevolucaoDosPet.Services.Interfaces;

namespace aRevolucaoDosPet.Services;
public class MongoTestService : IMongoTestService
{
    private readonly IMongoDatabase _database;

    public MongoTestService(IMongoDatabase database)
    {
        _database = database;
    }

    public string TestConnection()
    {
        try
        {
            var adminDb = _database.Client.GetDatabase("admin");
            var pingCommand = new BsonDocument("ping", 1);
            adminDb.RunCommand<BsonDocument>(pingCommand);
            return "Conexão com MongoDB estabelecida com sucesso!";
        }
        catch (Exception ex)
        {
            return $"Falha na conexão com MongoDB: {ex.Message}";
        }
    }
}