using aRevolucaoDosPet.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Config;

public class MongoIndexInitializer : IHostedService
{
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<UserSettings> _userSettings;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<PetFollower> _petFollowers;

    public MongoIndexInitializer(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
        _userSettings = database.GetCollection<UserSettings>("UserSettings");
        _pets = database.GetCollection<Pet>("Pets");
        _petFollowers = database.GetCollection<PetFollower>("PetFollowers");
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var legacyUpdatedAtFilter = new BsonDocumentFilterDefinition<User>(
            new BsonDocument("updated_at", BsonNull.Value));
        var setUpdatedAt = Builders<User>.Update.Set("updated_at", DateTime.UtcNow);
        await _users.UpdateManyAsync(
            legacyUpdatedAtFilter,
            setUpdatedAt,
            cancellationToken: cancellationToken);

        var indexes = new[]
        {
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(user => user.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Geo2DSphere(user => user.Location),
                new CreateIndexOptions { Name = "ix_users_location_2dsphere" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys
                    .Ascending(user => user.Status)
                    .Ascending(user => user.AccessLevel),
                new CreateIndexOptions { Name = "ix_users_status_access_level" })
        };

        await _users.Indexes.CreateManyAsync(indexes, cancellationToken);

        var userSettingsIndexes = new[]
        {
            new CreateIndexModel<UserSettings>(
                Builders<UserSettings>.IndexKeys.Ascending(settings => settings.UserId),
                new CreateIndexOptions { Unique = true, Name = "ux_user_settings_user_id" })
        };

        await _userSettings.Indexes.CreateManyAsync(userSettingsIndexes, cancellationToken);

        var petIndexes = new[]
        {
            new CreateIndexModel<Pet>(
                Builders<Pet>.IndexKeys.Ascending(pet => pet.OwnerId),
                new CreateIndexOptions { Name = "ix_pets_owner_id" }),
            new CreateIndexModel<Pet>(
                Builders<Pet>.IndexKeys.Ascending(pet => pet.Username),
                new CreateIndexOptions<Pet>
                {
                    Unique = true,
                    Name = "ux_pets_username",
                    PartialFilterExpression = new BsonDocumentFilterDefinition<Pet>(
                        new BsonDocument("username", new BsonDocument("$type", "string")))
                }),
            new CreateIndexModel<Pet>(
                Builders<Pet>.IndexKeys.Ascending(pet => pet.Species).Ascending(pet => pet.Breed),
                new CreateIndexOptions { Name = "ix_pets_species_breed" }),
            new CreateIndexModel<Pet>(
                Builders<Pet>.IndexKeys.Geo2DSphere(pet => pet.Location),
                new CreateIndexOptions { Name = "ix_pets_location_2dsphere" }),
            new CreateIndexModel<Pet>(
                Builders<Pet>.IndexKeys.Ascending(pet => pet.Status),
                new CreateIndexOptions { Name = "ix_pets_status" })
        };
        await _pets.Indexes.CreateManyAsync(petIndexes, cancellationToken);

        var petFollowerIndexes = new[]
        {
            new CreateIndexModel<PetFollower>(
                Builders<PetFollower>.IndexKeys.Ascending(follower => follower.PetId)
                    .Ascending(follower => follower.FollowerUserId),
                new CreateIndexOptions<PetFollower>
                {
                    Unique = true,
                    Name = "ux_pet_followers_user",
                    PartialFilterExpression = new BsonDocumentFilterDefinition<PetFollower>(
                        new BsonDocument("follower_user_id", new BsonDocument("$type", "objectId")))
                }),
            new CreateIndexModel<PetFollower>(
                Builders<PetFollower>.IndexKeys.Ascending(follower => follower.PetId)
                    .Ascending(follower => follower.FollowerPetId),
                new CreateIndexOptions<PetFollower>
                {
                    Unique = true,
                    Name = "ux_pet_followers_pet",
                    PartialFilterExpression = new BsonDocumentFilterDefinition<PetFollower>(
                        new BsonDocument("follower_pet_id", new BsonDocument("$type", "objectId")))
                })
        };
        await _petFollowers.Indexes.CreateManyAsync(petFollowerIndexes, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}