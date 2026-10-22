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
    private readonly IMongoCollection<Vaccine> _vaccines;
    private readonly IMongoCollection<Appointment> _appointments;
    private readonly IMongoCollection<WeightLog> _weightLogs;
    private readonly IMongoCollection<DiaryEntry> _diaryEntries;
    private readonly IMongoCollection<Expense> _expenses;
    private readonly IMongoCollection<HeatCycle> _heatCycles;
    private readonly IMongoCollection<Pregnancy> _pregnancies;
    private readonly IMongoCollection<Walk> _walks;
    private readonly IMongoCollection<WalkEncounter> _walkEncounters;
    private readonly IMongoCollection<Place> _places;
    private readonly IMongoCollection<PlaceReview> _placeReviews;
    private readonly IMongoCollection<Notification> _notifications;

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
        _vaccines = database.GetCollection<Vaccine>("Vaccines");
        _appointments = database.GetCollection<Appointment>("Appointments");
        _weightLogs = database.GetCollection<WeightLog>("WeightLogs");
        _diaryEntries = database.GetCollection<DiaryEntry>("DiaryEntries");
        _expenses = database.GetCollection<Expense>("Expenses");
        _heatCycles = database.GetCollection<HeatCycle>("HeatCycles");
        _pregnancies = database.GetCollection<Pregnancy>("Pregnancies");
        _walks = database.GetCollection<Walk>("Walks");
        _walkEncounters = database.GetCollection<WalkEncounter>("WalkEncounters");
        _places = database.GetCollection<Place>("Places");
        _placeReviews = database.GetCollection<PlaceReview>("PlaceReviews");
        _notifications = database.GetCollection<Notification>("Notifications");
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

        var vaccineIndexes = new[]
        {
            new CreateIndexModel<Vaccine>(
                Builders<Vaccine>.IndexKeys.Ascending(vaccine => vaccine.PetId),
                new CreateIndexOptions { Name = "ix_vaccines_pet_id" }),
            new CreateIndexModel<Vaccine>(
                Builders<Vaccine>.IndexKeys.Ascending(vaccine => vaccine.NextDoseAt),
                new CreateIndexOptions { Name = "ix_vaccines_next_dose_at" }),
            new CreateIndexModel<Vaccine>(
                Builders<Vaccine>.IndexKeys.Ascending(vaccine => vaccine.Status),
                new CreateIndexOptions { Name = "ix_vaccines_status" })
        };
        await _vaccines.Indexes.CreateManyAsync(vaccineIndexes, cancellationToken);

        var appointmentIndexes = new[]
        {
            new CreateIndexModel<Appointment>(
                Builders<Appointment>.IndexKeys.Ascending(appointment => appointment.PetId),
                new CreateIndexOptions { Name = "ix_appointments_pet_id" }),
            new CreateIndexModel<Appointment>(
                Builders<Appointment>.IndexKeys.Ascending(appointment => appointment.ScheduledAt),
                new CreateIndexOptions { Name = "ix_appointments_scheduled_at" }),
            new CreateIndexModel<Appointment>(
                Builders<Appointment>.IndexKeys.Ascending(appointment => appointment.Status),
                new CreateIndexOptions { Name = "ix_appointments_status" })
        };
        await _appointments.Indexes.CreateManyAsync(appointmentIndexes, cancellationToken);

        var weightLogIndexes = new[]
        {
            new CreateIndexModel<WeightLog>(
                Builders<WeightLog>.IndexKeys
                    .Ascending(log => log.PetId)
                    .Ascending(log => log.MeasuredAt),
                new CreateIndexOptions { Name = "ix_weight_logs_pet_id_measured_at" })
        };
        await _weightLogs.Indexes.CreateManyAsync(weightLogIndexes, cancellationToken);

        var diaryEntryIndexes = new[]
        {
            new CreateIndexModel<DiaryEntry>(
                Builders<DiaryEntry>.IndexKeys
                    .Ascending(entry => entry.PetId)
                    .Descending(entry => entry.EntryDate),
                new CreateIndexOptions { Name = "ix_diary_entries_pet_id_entry_date" }),
            new CreateIndexModel<DiaryEntry>(
                Builders<DiaryEntry>.IndexKeys.Ascending(entry => entry.Tags),
                new CreateIndexOptions { Name = "ix_diary_entries_tags" })
        };
        await _diaryEntries.Indexes.CreateManyAsync(diaryEntryIndexes, cancellationToken);

        var expenseIndexes = new[]
        {
            new CreateIndexModel<Expense>(
                Builders<Expense>.IndexKeys.Ascending(expense => expense.PetId).Ascending(expense => expense.ExpenseDate),
                new CreateIndexOptions { Name = "ix_expenses_pet_id_expense_date" }),
            new CreateIndexModel<Expense>(
                Builders<Expense>.IndexKeys.Ascending(expense => expense.PetId).Ascending(expense => expense.Category),
                new CreateIndexOptions { Name = "ix_expenses_pet_id_category" }),
            new CreateIndexModel<Expense>(
                Builders<Expense>.IndexKeys.Ascending(expense => expense.UserId),
                new CreateIndexOptions { Name = "ix_expenses_user_id" })
        };
        await _expenses.Indexes.CreateManyAsync(expenseIndexes, cancellationToken);

        var heatCycleIndexes = new[]
        {
            new CreateIndexModel<HeatCycle>(
                Builders<HeatCycle>.IndexKeys.Ascending(cycle => cycle.PetId).Ascending(cycle => cycle.StartedAt),
                new CreateIndexOptions { Name = "ix_heat_cycles_pet_id_started_at" })
        };
        await _heatCycles.Indexes.CreateManyAsync(heatCycleIndexes, cancellationToken);

        var pregnancyIndexes = new[]
        {
            new CreateIndexModel<Pregnancy>(
                Builders<Pregnancy>.IndexKeys.Ascending(pregnancy => pregnancy.PetId),
                new CreateIndexOptions { Name = "ix_pregnancies_pet_id" }),
            new CreateIndexModel<Pregnancy>(
                Builders<Pregnancy>.IndexKeys.Ascending(pregnancy => pregnancy.Status),
                new CreateIndexOptions { Name = "ix_pregnancies_status" })
        };
        await _pregnancies.Indexes.CreateManyAsync(pregnancyIndexes, cancellationToken);

        var walkIndexes = new[]
        {
            new CreateIndexModel<Walk>(Builders<Walk>.IndexKeys.Ascending(walk => walk.PetId), new CreateIndexOptions { Name = "ix_walks_pet_id" }),
            new CreateIndexModel<Walk>(Builders<Walk>.IndexKeys.Ascending(walk => walk.UserId), new CreateIndexOptions { Name = "ix_walks_user_id" }),
            new CreateIndexModel<Walk>(Builders<Walk>.IndexKeys.Ascending(walk => walk.StartedAt), new CreateIndexOptions { Name = "ix_walks_started_at" }),
            new CreateIndexModel<Walk>(Builders<Walk>.IndexKeys.Geo2DSphere(walk => walk.Route), new CreateIndexOptions { Name = "ix_walks_route_2dsphere" })
        };
        await _walks.Indexes.CreateManyAsync(walkIndexes, cancellationToken);

        var walkEncounterIndexes = new[]
        {
            new CreateIndexModel<WalkEncounter>(Builders<WalkEncounter>.IndexKeys.Ascending(encounter => encounter.WalkId), new CreateIndexOptions { Name = "ix_walk_encounters_walk_id" }),
            new CreateIndexModel<WalkEncounter>(Builders<WalkEncounter>.IndexKeys.Geo2DSphere(encounter => encounter.Location), new CreateIndexOptions { Name = "ix_walk_encounters_location_2dsphere" })
        };
        await _walkEncounters.Indexes.CreateManyAsync(walkEncounterIndexes, cancellationToken);

        var placeIndexes = new[]
        {
            new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Geo2DSphere(place => place.Location), new CreateIndexOptions { Name = "ix_places_location_2dsphere" }),
            new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Ascending(place => place.Type), new CreateIndexOptions { Name = "ix_places_type" }),
            new CreateIndexModel<Place>(Builders<Place>.IndexKeys.Descending(place => place.AverageRating), new CreateIndexOptions { Name = "ix_places_average_rating" })
        };
        await _places.Indexes.CreateManyAsync(placeIndexes, cancellationToken);

        var placeReviewIndexes = new[]
        {
            new CreateIndexModel<PlaceReview>(Builders<PlaceReview>.IndexKeys.Ascending(review => review.PlaceId).Ascending(review => review.UserId),
                new CreateIndexOptions { Unique = true, Name = "ux_place_reviews_place_user" }),
            new CreateIndexModel<PlaceReview>(Builders<PlaceReview>.IndexKeys.Ascending(review => review.PlaceId), new CreateIndexOptions { Name = "ix_place_reviews_place_id" })
        };
        await _placeReviews.Indexes.CreateManyAsync(placeReviewIndexes, cancellationToken);

        var notificationIndexes = new[]
        {
            new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending(notification => notification.UserId).Ascending(notification => notification.IsRead),
                new CreateIndexOptions { Name = "ix_notifications_user_is_read" }),
            new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Descending(notification => notification.CreatedAt),
                new CreateIndexOptions { Name = "ix_notifications_created_at" })
        };
        await _notifications.Indexes.CreateManyAsync(notificationIndexes, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}