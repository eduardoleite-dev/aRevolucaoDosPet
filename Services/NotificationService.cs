using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class NotificationService : INotificationService
{
    private readonly IMongoCollection<Notification> _notifications;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<Pet> _pets;

    public NotificationService(IMongoDatabase database)
    {
        _notifications = database.GetCollection<Notification>("Notifications");
        _users = database.GetCollection<User>("Users");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<NotificationResponseDto> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.UserId, "userId");
        if (!await _users.Find(user => user.Id == request.UserId && user.Status == UserStatus.ACTIVE).AnyAsync(cancellationToken))
            throw new NotificationUserNotFoundException();

        var petId = PetRecordHelper.NormalizeOptional(request.PetId);
        if (petId is not null)
        {
            PetRecordHelper.EnsureObjectId(petId, "petId");
            if (!await _pets.Find(pet => pet.Id == petId && pet.DeletedAt == null).AnyAsync(cancellationToken))
                throw new NotificationPetNotFoundException();
        }

        var notification = new Notification
        {
            UserId = request.UserId,
            PetId = petId,
            Type = request.Type,
            Title = request.Title.Trim(),
            Body = request.Body.Trim(),
            Data = GeoJsonMapper.ToBsonData(request.Data),
            CreatedAt = DateTime.UtcNow
        };
        await _notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
        return ToResponse(notification);
    }

    public async Task<PagedResult<NotificationResponseDto>> SearchAsync(
        NotificationSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Notification>>();
        var builder = Builders<Notification>.Filter;
        var userId = filter.UserId;
        if (!isAdministrator)
        {
            PetRecordHelper.EnsureObjectId(currentUserId, "userId");
            if (!string.IsNullOrWhiteSpace(userId) && userId != currentUserId) throw new RecordPermissionException();
            userId = currentUserId;
        }

        PetRecordHelper.AddObjectId(filters, "user_id", userId);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        if (filter.Type.HasValue) filters.Add(builder.Eq(item => item.Type, filter.Type.Value));
        if (filter.IsRead.HasValue) filters.Add(builder.Eq(item => item.IsRead, filter.IsRead.Value));
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "read_at", filter.ReadAtFrom, filter.ReadAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var count = await _notifications.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var items = await _notifications.Find(query).SortByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
            .Skip(skip).Limit(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<NotificationResponseDto>(items.Select(ToResponse).ToList(), count, filter.PageNumber, filter.PageSize);
    }

    public async Task<NotificationResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var item = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return item is null ? null : ToResponse(item);
    }

    public async Task<bool> MarkReadAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var item = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (item is null) return false;
        if (item.IsRead) return true;

        var result = await _notifications.UpdateOneAsync(
            notification => notification.Id == id && !notification.IsRead,
            Builders<Notification>.Update.Set(notification => notification.IsRead, true).Set(notification => notification.ReadAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var item = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (item is null) return false;
        return (await _notifications.DeleteOneAsync(notification => notification.Id == id, cancellationToken)).DeletedCount == 1;
    }

    private async Task<Notification?> FindAuthorizedAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var item = await _notifications.Find(notification => notification.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (item is null) return null;
        if (!isAdministrator && item.UserId != currentUserId) throw new RecordPermissionException();
        return item;
    }

    private static NotificationResponseDto ToResponse(Notification notification) => new(
        notification.Id,
        notification.UserId,
        notification.PetId,
        notification.Type,
        notification.Title,
        notification.Body,
        GeoJsonMapper.ToDataDto(notification.Data),
        notification.IsRead,
        notification.ReadAt,
        notification.CreatedAt);
}
