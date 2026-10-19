using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class UserSettingsService : IUserSettingsService
{
    private readonly IMongoCollection<UserSettings> _settings;
    private readonly IMongoCollection<User> _users;

    public UserSettingsService(IMongoDatabase database)
    {
        _settings = database.GetCollection<UserSettings>("UserSettings");
        _users = database.GetCollection<User>("Users");
    }

    public async Task<UserSettingsResponseDto> CreateAsync(
        string userId,
        UserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidObjectId(userId);
        var userExists = await _users.Find(user =>
                user.Id == userId && user.Status != UserStatus.DELETED)
            .AnyAsync(cancellationToken);
        if (!userExists)
        {
            throw new SettingsUserNotFoundException();
        }

        var now = DateTime.UtcNow;
        var settings = new UserSettings
        {
            UserId = userId,
            Notifications = ToModel(request.Notifications!),
            Privacy = ToModel(request.Privacy!),
            Theme = request.Theme.Trim(),
            Language = request.Language.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        try
        {
            await _settings.InsertOneAsync(settings, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new UserSettingsAlreadyExistsException();
        }

        return ToResponse(settings);
    }

    public async Task<UserSettingsResponseDto?> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(userId, out _))
        {
            return null;
        }

        var settings = await _settings.Find(item => item.UserId == userId).FirstOrDefaultAsync(cancellationToken);
        return settings is null ? null : ToResponse(settings);
    }

    public async Task<UserSettingsResponseDto?> UpdateAsync(
        string userId,
        UserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(userId, out _))
        {
            return null;
        }

        var update = Builders<UserSettings>.Update
            .Set(item => item.Notifications, ToModel(request.Notifications!))
            .Set(item => item.Privacy, ToModel(request.Privacy!))
            .Set(item => item.Theme, request.Theme.Trim())
            .Set(item => item.Language, request.Language.Trim())
            .Set(item => item.UpdatedAt, DateTime.UtcNow);

        var settings = await _settings.FindOneAndUpdateAsync(
            item => item.UserId == userId,
            update,
            new FindOneAndUpdateOptions<UserSettings> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return settings is null ? null : ToResponse(settings);
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(userId, out _))
        {
            return false;
        }

        var result = await _settings.DeleteOneAsync(item => item.UserId == userId, cancellationToken);
        return result.DeletedCount == 1;
    }

    private static void EnsureValidObjectId(string userId)
    {
        if (!ObjectId.TryParse(userId, out _))
        {
            throw new ArgumentException("O userId informado não é um ObjectId válido.");
        }
    }

    private static NotificationSettings ToModel(NotificationSettingsDto dto) => new()
    {
        VaccineDue = dto.VaccineDue,
        Appointment = dto.Appointment,
        HeatCycle = dto.HeatCycle,
        Likes = dto.Likes,
        Comments = dto.Comments,
        Followers = dto.Followers,
        Nearby = dto.Nearby,
        Expenses = dto.Expenses
    };

    private static PrivacySettings ToModel(PrivacySettingsDto dto) => new()
    {
        ShowLocation = dto.ShowLocation,
        AllowDm = dto.AllowDm,
        PublicProfile = dto.PublicProfile
    };

    private static UserSettingsResponseDto ToResponse(UserSettings settings) => new(
        settings.Id,
        settings.UserId,
        new NotificationSettingsDto
        {
            VaccineDue = settings.Notifications.VaccineDue,
            Appointment = settings.Notifications.Appointment,
            HeatCycle = settings.Notifications.HeatCycle,
            Likes = settings.Notifications.Likes,
            Comments = settings.Notifications.Comments,
            Followers = settings.Notifications.Followers,
            Nearby = settings.Notifications.Nearby,
            Expenses = settings.Notifications.Expenses
        },
        new PrivacySettingsDto
        {
            ShowLocation = settings.Privacy.ShowLocation,
            AllowDm = settings.Privacy.AllowDm,
            PublicProfile = settings.Privacy.PublicProfile
        },
        settings.Theme,
        settings.Language,
        settings.CreatedAt,
        settings.UpdatedAt);
}