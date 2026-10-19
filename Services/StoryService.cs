using System.Text.RegularExpressions;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class StoryService : IStoryService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private readonly IMongoCollection<Story> _stories;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<PetFollower> _petFollowers;

    public StoryService(IMongoDatabase database)
    {
        _stories = database.GetCollection<Story>("Stories");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
        _petFollowers = database.GetCollection<PetFollower>("PetFollowers");
    }

    public async Task<StoryResponseDto> CreateAsync(
        CreateStoryRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var authorUserId = isAdministrator && !string.IsNullOrWhiteSpace(request.AuthorUserId)
            ? request.AuthorUserId
            : currentUserId;
        EnsureObjectId(authorUserId, "authorUserId");
        EnsureObjectId(request.PetId, "petId");

        var pet = await _pets.Find(item => item.Id == request.PetId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new StoryPetNotFoundException();
        if (!isAdministrator && !CanEdit(pet, currentUserId)) throw new StoryPermissionException();

        var authorExists = await _users.Find(user => user.Id == authorUserId && user.Status == UserStatus.ACTIVE)
            .AnyAsync(cancellationToken);
        if (!authorExists) throw new StoryAuthorNotFoundException();

        var now = DateTime.UtcNow;
        var story = new Story
        {
            PetId = request.PetId,
            AuthorUserId = authorUserId!,
            MediaUrl = request.MediaUrl.Trim(),
            MediaType = request.MediaType,
            Caption = NormalizeOptional(request.Caption),
            CreatedAt = now,
            ExpiresAt = now.Add(Lifetime)
        };

        await _stories.InsertOneAsync(story, cancellationToken: cancellationToken);
        return ToResponse(story);
    }

    public async Task<PagedResult<StoryResponseDto>?> SearchAsync(
        StorySearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var hasPetId = !string.IsNullOrWhiteSpace(filter.PetId);
        if (!isAdministrator && !hasPetId) throw new ArgumentException("Informe o petId.");

        if (hasPetId)
        {
            EnsureObjectId(filter.PetId, "petId");
            if (!isAdministrator && !await CanViewPetAsync(filter.PetId!, viewerUserId, cancellationToken))
            {
                return null;
            }
        }

        var filters = new List<FilterDefinition<Story>>();
        var builder = Builders<Story>.Filter;

        AddTextFilter(filters, "caption", filter.Caption);
        AddObjectIdFilter(filters, "_id", filter.Id);
        AddObjectIdFilter(filters, "pet_id", filter.PetId);
        AddObjectIdFilter(filters, "author_user_id", filter.AuthorUserId);

        if (!string.IsNullOrWhiteSpace(filter.MediaType))
        {
            filters.Add(builder.Eq(story => story.MediaType, filter.MediaType.Trim().ToLowerInvariant()));
        }

        if (filter.MinViewsCount.HasValue) filters.Add(builder.Gte(story => story.ViewsCount, filter.MinViewsCount.Value));
        if (filter.MaxViewsCount.HasValue) filters.Add(builder.Lte(story => story.ViewsCount, filter.MaxViewsCount.Value));
        AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        AddDateRange(filters, "expires_at", filter.ExpiresAtFrom, filter.ExpiresAtTo);

        if (!(isAdministrator && filter.IncludeExpired))
        {
            filters.Add(builder.Gt(story => story.ExpiresAt, DateTime.UtcNow));
        }

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _stories.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var stories = await _stories.Find(query)
            .SortByDescending(story => story.CreatedAt)
            .ThenBy(story => story.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StoryResponseDto>(
            stories.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<StoryResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var story = await _stories.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (story is null) return null;
        if (isAdministrator) return ToResponse(story);
        if (story.ExpiresAt <= DateTime.UtcNow) return null;

        return await CanViewPetAsync(story.PetId, viewerUserId, cancellationToken) ? ToResponse(story) : null;
    }

    public async Task<StoryResponseDto?> UpdateAsync(
        string id,
        UpdateStoryRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindEditableStoryAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (existing is null) return null;

        var updated = await _stories.FindOneAndUpdateAsync(
            story => story.Id == id,
            Builders<Story>.Update.Set(story => story.Caption, NormalizeOptional(request.Caption)),
            new FindOneAndUpdateOptions<Story> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return updated is null ? null : ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindEditableStoryAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (existing is null) return false;

        var result = await _stories.DeleteOneAsync(story => story.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    public async Task<StoryResponseDto?> RegisterViewAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var story = await GetByIdAsync(id, viewerUserId, isAdministrator, cancellationToken);
        if (story is null) return null;

        // Visualizações do próprio tutor ou de contas sem usuário no banco não contam.
        if (!ObjectId.TryParse(viewerUserId, out _) ||
            await IsPetEditorAsync(story.PetId, viewerUserId, cancellationToken))
        {
            return story;
        }

        var updated = await _stories.FindOneAndUpdateAsync(
            item => item.Id == id,
            Builders<Story>.Update.Inc(item => item.ViewsCount, 1),
            new FindOneAndUpdateOptions<Story> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        return updated is null ? null : ToResponse(updated);
    }

    private async Task<Story?> FindEditableStoryAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var story = await _stories.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (story is null) return null;
        if (isAdministrator) return story;
        if (story.ExpiresAt <= DateTime.UtcNow) return null;
        if (!await IsPetEditorAsync(story.PetId, currentUserId, cancellationToken)) throw new StoryPermissionException();
        return story;
    }

    private async Task<bool> CanViewPetAsync(string petId, string? viewerUserId, CancellationToken cancellationToken)
    {
        var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) return false;
        if (pet.Visibility == PetProfileVisibility.PUBLIC) return true;
        if (!ObjectId.TryParse(viewerUserId, out _)) return false;
        if (CanEdit(pet, viewerUserId)) return true;

        return pet.Visibility == PetProfileVisibility.FOLLOWERS_ONLY &&
               await _petFollowers
                   .Find(follower => follower.PetId == petId && follower.FollowerUserId == viewerUserId)
                   .AnyAsync(cancellationToken);
    }

    private async Task<bool> IsPetEditorAsync(string petId, string? userId, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var userObjectId)) return false;
        return await _pets
            .Find(pet => pet.Id == petId && pet.DeletedAt == null &&
                         (pet.OwnerId == userId || pet.CoTutorIds.Contains(userObjectId)))
            .AnyAsync(cancellationToken);
    }

    private static bool CanEdit(Pet pet, string? userId) =>
        userId is not null &&
        (pet.OwnerId == userId ||
         (ObjectId.TryParse(userId, out var objectId) && pet.CoTutorIds.Contains(objectId)));

    private static void EnsureObjectId(string? id, string field)
    {
        if (!ObjectId.TryParse(id, out _)) throw new ArgumentException($"O {field} informado não é um ObjectId válido.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddTextFilter(List<FilterDefinition<Story>> filters, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(Builders<Story>.Filter.Regex(field, new BsonRegularExpression(Regex.Escape(value.Trim()), "i")));
        }
    }

    private static void AddObjectIdFilter(List<FilterDefinition<Story>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<Story>(new BsonDocument(field, objectId)));
    }

    private static void AddDateRange(List<FilterDefinition<Story>> filters, string field, DateTime? from, DateTime? to)
    {
        if (from.HasValue) filters.Add(Builders<Story>.Filter.Gte(field, from.Value));
        if (to.HasValue) filters.Add(Builders<Story>.Filter.Lte(field, to.Value));
    }

    private static StoryResponseDto ToResponse(Story story) => new(
        story.Id,
        story.PetId,
        story.AuthorUserId,
        story.MediaUrl,
        story.MediaType,
        story.Caption,
        story.ViewsCount,
        story.ExpiresAt,
        story.CreatedAt);
}
