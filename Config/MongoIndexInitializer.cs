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
    private readonly IMongoCollection<Post> _posts;
    private readonly IMongoCollection<PostReaction> _postReactions;
    private readonly IMongoCollection<Comment> _comments;
    private readonly IMongoCollection<Story> _stories;

    public MongoIndexInitializer(IMongoDatabase database)
    {
        _users = database.GetCollection<User>("Users");
        _userSettings = database.GetCollection<UserSettings>("UserSettings");
        _pets = database.GetCollection<Pet>("Pets");
        _petFollowers = database.GetCollection<PetFollower>("PetFollowers");
        _posts = database.GetCollection<Post>("Posts");
        _postReactions = database.GetCollection<PostReaction>("PostReactions");
        _comments = database.GetCollection<Comment>("Comments");
        _stories = database.GetCollection<Story>("Stories");
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

        var postIndexes = new[]
        {
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Ascending(post => post.PetId),
                new CreateIndexOptions { Name = "ix_posts_pet_id" }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Descending(post => post.CreatedAt),
                new CreateIndexOptions { Name = "ix_posts_created_at" }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Geo2DSphere(post => post.Location),
                new CreateIndexOptions { Name = "ix_posts_location_2dsphere" }),
            new CreateIndexModel<Post>(
                Builders<Post>.IndexKeys.Ascending(post => post.Hashtags),
                new CreateIndexOptions { Name = "ix_posts_hashtags" })
        };
        await _posts.Indexes.CreateManyAsync(postIndexes, cancellationToken);

        var postReactionIndexes = new[]
        {
            new CreateIndexModel<PostReaction>(
                Builders<PostReaction>.IndexKeys
                    .Ascending(reaction => reaction.PostId)
                    .Ascending(reaction => reaction.UserId)
                    .Ascending(reaction => reaction.PetId),
                new CreateIndexOptions { Unique = true, Name = "ux_post_reactions_post_user_pet" })
        };
        await _postReactions.Indexes.CreateManyAsync(postReactionIndexes, cancellationToken);

        var commentIndexes = new[]
        {
            new CreateIndexModel<Comment>(
                Builders<Comment>.IndexKeys.Ascending(comment => comment.PostId),
                new CreateIndexOptions { Name = "ix_comments_post_id" }),
            new CreateIndexModel<Comment>(
                Builders<Comment>.IndexKeys.Ascending(comment => comment.ParentId),
                new CreateIndexOptions { Name = "ix_comments_parent_id" })
        };
        await _comments.Indexes.CreateManyAsync(commentIndexes, cancellationToken);

        var storyIndexes = new[]
        {
            new CreateIndexModel<Story>(
                Builders<Story>.IndexKeys.Ascending(story => story.PetId),
                new CreateIndexOptions { Name = "ix_stories_pet_id" }),
            new CreateIndexModel<Story>(
                Builders<Story>.IndexKeys.Ascending(story => story.ExpiresAt),
                new CreateIndexOptions { Name = "ttl_stories_expires_at", ExpireAfter = TimeSpan.Zero })
        };
        await _stories.Indexes.CreateManyAsync(storyIndexes, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}