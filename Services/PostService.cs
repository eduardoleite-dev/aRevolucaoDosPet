using System.Text.RegularExpressions;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PostService : IPostService
{
    private const int MaxCarouselItems = 10;
    private const int MaxListItems = 30;
    private static readonly Regex HashtagPattern = new(@"^[\p{L}\p{N}_]{1,50}$", RegexOptions.Compiled);

    private readonly IMongoCollection<Post> _posts;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<PetFollower> _petFollowers;
    private readonly IMongoCollection<PostReaction> _reactions;
    private readonly IMongoCollection<Comment> _comments;

    public PostService(IMongoDatabase database)
    {
        _posts = database.GetCollection<Post>("Posts");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
        _petFollowers = database.GetCollection<PetFollower>("PetFollowers");
        _reactions = database.GetCollection<PostReaction>("PostReactions");
        _comments = database.GetCollection<Comment>("Comments");
    }

    public async Task<PostResponseDto> CreateAsync(
        CreatePostRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var authorUserId = isAdministrator && !string.IsNullOrWhiteSpace(request.AuthorUserId)
            ? request.AuthorUserId
            : currentUserId;
        EnsureObjectId(authorUserId, "authorUserId");
        EnsureObjectId(request.PetId, "petId");
        ValidateContent(request.Type, request.Media, request.Caption);

        var pet = await _pets.Find(item => item.Id == request.PetId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new PostPetNotFoundException();
        if (!isAdministrator && !CanEdit(pet, currentUserId)) throw new PostPermissionException();

        var authorExists = await _users.Find(user => user.Id == authorUserId && user.Status == UserStatus.ACTIVE)
            .AnyAsync(cancellationToken);
        if (!authorExists) throw new PostAuthorNotFoundException();

        var now = DateTime.UtcNow;
        var post = new Post
        {
            PetId = request.PetId,
            AuthorUserId = authorUserId!,
            Type = request.Type,
            Caption = NormalizeOptional(request.Caption),
            Media = MapMedia(request.Media),
            Location = MapLocation(request.Location),
            TaggedPetIds = await ValidateTaggedPetsAsync(request.TaggedPetIds, cancellationToken),
            TaggedUserIds = await ValidateTaggedUsersAsync(request.TaggedUserIds, cancellationToken),
            Hashtags = NormalizeHashtags(request.Hashtags),
            Visibility = request.Visibility,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _posts.InsertOneAsync(post, cancellationToken: cancellationToken);
        await _pets.UpdateOneAsync(
            item => item.Id == pet.Id,
            Builders<Pet>.Update.Inc(item => item.PostsCount, 1),
            cancellationToken: cancellationToken);

        return ToResponse(post);
    }

    public async Task<PagedResult<PostResponseDto>> SearchAsync(
        PostSearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Post>>();
        var builder = Builders<Post>.Filter;

        AddTextFilter(filters, "caption", filter.Caption);
        AddTextFilter(filters, "location.name", filter.LocationName);
        AddTextFilter(filters, "location.place_id", filter.PlaceId);
        AddObjectIdFilter(filters, "_id", filter.Id);
        AddObjectIdFilter(filters, "pet_id", filter.PetId);
        AddObjectIdFilter(filters, "author_user_id", filter.AuthorUserId);
        AddObjectIdFilter(filters, "tagged_pet_ids", filter.TaggedPetId);
        AddObjectIdFilter(filters, "tagged_user_ids", filter.TaggedUserId);

        if (!string.IsNullOrWhiteSpace(filter.MediaType))
        {
            filters.Add(builder.Eq("media.type", filter.MediaType.Trim().ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Hashtag))
        {
            filters.Add(builder.Eq("hashtags", filter.Hashtag.Trim().TrimStart('#').ToLowerInvariant()));
        }

        if (filter.Type.HasValue) filters.Add(builder.Eq(post => post.Type, filter.Type.Value));
        if (filter.Visibility.HasValue) filters.Add(builder.Eq(post => post.Visibility, filter.Visibility.Value));
        if (filter.HasLocation.HasValue)
        {
            filters.Add(filter.HasLocation.Value
                ? builder.Ne(post => post.Location, null)
                : builder.Eq(post => post.Location, null));
        }

        if (filter.MinLikesCount.HasValue) filters.Add(builder.Gte(post => post.LikesCount, filter.MinLikesCount.Value));
        if (filter.MaxLikesCount.HasValue) filters.Add(builder.Lte(post => post.LikesCount, filter.MaxLikesCount.Value));
        if (filter.MinCommentsCount.HasValue) filters.Add(builder.Gte(post => post.CommentsCount, filter.MinCommentsCount.Value));
        if (filter.MaxCommentsCount.HasValue) filters.Add(builder.Lte(post => post.CommentsCount, filter.MaxCommentsCount.Value));
        if (filter.MinSharesCount.HasValue) filters.Add(builder.Gte(post => post.SharesCount, filter.MinSharesCount.Value));
        if (filter.MaxSharesCount.HasValue) filters.Add(builder.Lte(post => post.SharesCount, filter.MaxSharesCount.Value));

        if (filter.Longitude.HasValue != filter.Latitude.HasValue)
        {
            throw new ArgumentException("Informe longitude e latitude juntas.");
        }

        if (filter.RadiusMeters.HasValue && !filter.Longitude.HasValue)
        {
            throw new ArgumentException("Informe longitude e latitude para filtrar por proximidade.");
        }

        if (filter.RadiusMeters.HasValue)
        {
            var radiusInRadians = filter.RadiusMeters.Value / 6_378_100d;
            filters.Add(new BsonDocumentFilterDefinition<Post>(new BsonDocument("location", new BsonDocument(
                "$geoWithin",
                new BsonDocument("$centerSphere", new BsonArray
                {
                    new BsonArray { filter.Longitude!.Value, filter.Latitude!.Value },
                    radiusInRadians
                })))));
        }
        else
        {
            if (filter.Longitude.HasValue) filters.Add(builder.Eq("location.coordinates.0", filter.Longitude.Value));
            if (filter.Latitude.HasValue) filters.Add(builder.Eq("location.coordinates.1", filter.Latitude.Value));
        }

        AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);
        AddDateRange(filters, "deleted_at", filter.DeletedAtFrom, filter.DeletedAtTo);

        if (!(isAdministrator && filter.IncludeDeleted))
        {
            filters.Add(builder.Eq(post => post.DeletedAt, null));
        }

        if (!isAdministrator)
        {
            filters.Add(await BuildVisibilityFilterAsync(viewerUserId, cancellationToken));
        }

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _posts.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var posts = await _posts.Find(query)
            .SortByDescending(post => post.CreatedAt)
            .ThenBy(post => post.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PostResponseDto>(posts.Select(ToResponse).ToList(), totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<PostResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var post = await _posts.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (post is null) return null;
        if (isAdministrator) return ToResponse(post);
        if (post.DeletedAt.HasValue) return null;

        return await CanViewAsync(post, viewerUserId, cancellationToken) ? ToResponse(post) : null;
    }

    public async Task<PostResponseDto?> UpdateAsync(
        string id,
        PostWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var existing = await _posts.Find(item => item.Id == id && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null) return null;
        if (!isAdministrator && !await IsPetEditorAsync(existing.PetId, currentUserId, cancellationToken))
        {
            throw new PostPermissionException();
        }

        if (existing.Type == PostType.TEXT && string.IsNullOrWhiteSpace(request.Caption))
        {
            throw new ArgumentException("Posts de texto exigem legenda.");
        }

        var update = Builders<Post>.Update
            .Set(post => post.Caption, NormalizeOptional(request.Caption))
            .Set(post => post.Location, MapLocation(request.Location))
            .Set(post => post.TaggedPetIds, await ValidateTaggedPetsAsync(request.TaggedPetIds, cancellationToken))
            .Set(post => post.TaggedUserIds, await ValidateTaggedUsersAsync(request.TaggedUserIds, cancellationToken))
            .Set(post => post.Hashtags, NormalizeHashtags(request.Hashtags))
            .Set(post => post.Visibility, request.Visibility)
            .Set(post => post.UpdatedAt, DateTime.UtcNow);

        var updated = await _posts.FindOneAndUpdateAsync(
            post => post.Id == id && post.DeletedAt == null,
            update,
            new FindOneAndUpdateOptions<Post> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        return updated is null ? null : ToResponse(updated);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return false;

        var existing = await _posts.Find(item => item.Id == id && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null) return false;
        if (!isAdministrator && !await IsPetEditorAsync(existing.PetId, currentUserId, cancellationToken))
        {
            throw new PostPermissionException();
        }

        var now = DateTime.UtcNow;
        var result = await _posts.UpdateOneAsync(
            post => post.Id == id && post.DeletedAt == null,
            Builders<Post>.Update.Set(post => post.DeletedAt, now).Set(post => post.UpdatedAt, now),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0) return false;

        await _pets.UpdateOneAsync(
            pet => pet.Id == existing.PetId,
            Builders<Pet>.Update.Inc(pet => pet.PostsCount, -1),
            cancellationToken: cancellationToken);
        await _reactions.DeleteManyAsync(reaction => reaction.PostId == id, cancellationToken);
        await _comments.UpdateManyAsync(
            comment => comment.PostId == id && comment.DeletedAt == null,
            Builders<Comment>.Update.Set(comment => comment.DeletedAt, now).Set(comment => comment.UpdatedAt, now),
            cancellationToken: cancellationToken);
        return true;
    }

    private async Task<FilterDefinition<Post>> BuildVisibilityFilterAsync(
        string? viewerUserId,
        CancellationToken cancellationToken)
    {
        var builder = Builders<Post>.Filter;
        var publicPosts = builder.Eq(post => post.Visibility, PostVisibility.PUBLIC);
        if (!ObjectId.TryParse(viewerUserId, out var viewerObjectId)) return publicPosts;

        var editablePetIds = await _pets
            .Find(pet => pet.OwnerId == viewerUserId || pet.CoTutorIds.Contains(viewerObjectId))
            .Project(pet => pet.Id)
            .ToListAsync(cancellationToken);
        var followedPetIds = await _petFollowers
            .Find(follower => follower.FollowerUserId == viewerUserId)
            .Project(follower => follower.PetId)
            .ToListAsync(cancellationToken);

        var visible = new List<FilterDefinition<Post>> { publicPosts };
        if (editablePetIds.Count > 0)
        {
            visible.Add(builder.In(post => post.PetId, editablePetIds));
        }

        if (followedPetIds.Count > 0)
        {
            visible.Add(builder.Eq(post => post.Visibility, PostVisibility.FOLLOWERS_ONLY) &
                        builder.In(post => post.PetId, followedPetIds));
        }

        return builder.Or(visible);
    }

    private async Task<bool> CanViewAsync(Post post, string? viewerUserId, CancellationToken cancellationToken)
    {
        if (post.Visibility == PostVisibility.PUBLIC) return true;
        if (!ObjectId.TryParse(viewerUserId, out _)) return false;
        if (await IsPetEditorAsync(post.PetId, viewerUserId, cancellationToken)) return true;

        return post.Visibility == PostVisibility.FOLLOWERS_ONLY &&
               await _petFollowers
                   .Find(follower => follower.PetId == post.PetId && follower.FollowerUserId == viewerUserId)
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

    private async Task<List<ObjectId>> ValidateTaggedPetsAsync(
        IEnumerable<string>? ids,
        CancellationToken cancellationToken)
    {
        var objectIds = ParseObjectIds(ids, "taggedPetIds");
        if (objectIds.Count == 0) return objectIds;

        var count = await _pets.CountDocumentsAsync(
            Builders<Pet>.Filter.In("_id", objectIds) & Builders<Pet>.Filter.Eq(pet => pet.DeletedAt, null),
            cancellationToken: cancellationToken);
        if (count != objectIds.Count) throw new ArgumentException("Todos os pets marcados devem existir.");
        return objectIds;
    }

    private async Task<List<ObjectId>> ValidateTaggedUsersAsync(
        IEnumerable<string>? ids,
        CancellationToken cancellationToken)
    {
        var objectIds = ParseObjectIds(ids, "taggedUserIds");
        if (objectIds.Count == 0) return objectIds;

        var count = await _users.CountDocumentsAsync(
            Builders<User>.Filter.In("_id", objectIds) & Builders<User>.Filter.Eq(user => user.Status, UserStatus.ACTIVE),
            cancellationToken: cancellationToken);
        if (count != objectIds.Count) throw new ArgumentException("Todos os usuários marcados devem existir e estar ativos.");
        return objectIds;
    }

    private static List<ObjectId> ParseObjectIds(IEnumerable<string>? ids, string field)
    {
        if (ids is null) return [];

        var result = new List<ObjectId>();
        foreach (var id in ids.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                throw new ArgumentException($"{field} contém um id inválido.");
            }

            result.Add(objectId);
        }

        if (result.Count > MaxListItems) throw new ArgumentException($"{field} aceita no máximo {MaxListItems} itens.");
        return result;
    }

    private static List<string> NormalizeHashtags(IEnumerable<string>? hashtags)
    {
        if (hashtags is null) return [];

        var result = new List<string>();
        foreach (var hashtag in hashtags)
        {
            var normalized = hashtag.Trim().TrimStart('#').ToLowerInvariant();
            if (normalized.Length == 0) continue;
            if (!HashtagPattern.IsMatch(normalized))
            {
                throw new ArgumentException($"Hashtag inválida: {hashtag}");
            }

            if (!result.Contains(normalized)) result.Add(normalized);
        }

        if (result.Count > MaxListItems) throw new ArgumentException($"São permitidas no máximo {MaxListItems} hashtags.");
        return result;
    }

    private static void ValidateContent(PostType type, IReadOnlyList<PostMediaDto> media, string? caption)
    {
        if (type == PostType.TEXT)
        {
            if (string.IsNullOrWhiteSpace(caption)) throw new ArgumentException("Posts de texto exigem legenda.");
            if (media.Count > 0) throw new ArgumentException("Posts de texto não aceitam mídia.");
            return;
        }

        if (type == PostType.CAROUSEL)
        {
            if (media.Count is < 2 or > MaxCarouselItems)
            {
                throw new ArgumentException($"Carrosséis exigem de 2 a {MaxCarouselItems} mídias.");
            }

            if (media.Any(item => item.Type == "audio"))
            {
                throw new ArgumentException("Carrosséis aceitam somente imagens e vídeos.");
            }

            return;
        }

        var expectedMediaType = type switch
        {
            PostType.PHOTO => "image",
            PostType.VIDEO => "video",
            _ => "audio"
        };
        if (media.Count != 1 || media[0].Type != expectedMediaType)
        {
            throw new ArgumentException($"Este tipo de post exige exatamente uma mídia do tipo {expectedMediaType}.");
        }
    }

    private static List<PostMedia> MapMedia(IEnumerable<PostMediaDto> media) =>
        media.Select(item => new PostMedia
        {
            Url = item.Url.Trim(),
            PublicId = NormalizeOptional(item.PublicId),
            Type = item.Type,
            Width = item.Width,
            Height = item.Height,
            DurationSeconds = item.DurationSeconds
        }).ToList();

    private static PostLocation? MapLocation(PostLocationRequest? location)
    {
        if (location is null) return null;

        var coordinates = location.Coordinates;
        if (coordinates.Length != 2 || !double.IsFinite(coordinates[0]) || !double.IsFinite(coordinates[1]) ||
            coordinates[0] is < -180 or > 180 || coordinates[1] is < -90 or > 90)
        {
            throw new ArgumentException("A localização deve conter [longitude, latitude] em limites válidos.");
        }

        return new PostLocation
        {
            Coordinates = coordinates,
            Name = NormalizeOptional(location.Name),
            PlaceId = NormalizeOptional(location.PlaceId)
        };
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

    private static void AddTextFilter(List<FilterDefinition<Post>> filters, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(Builders<Post>.Filter.Regex(field, new BsonRegularExpression(Regex.Escape(value.Trim()), "i")));
        }
    }

    private static void AddObjectIdFilter(List<FilterDefinition<Post>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<Post>(new BsonDocument(field, objectId)));
    }

    private static void AddDateRange(List<FilterDefinition<Post>> filters, string field, DateTime? from, DateTime? to)
    {
        if (from.HasValue) filters.Add(Builders<Post>.Filter.Gte(field, from.Value));
        if (to.HasValue) filters.Add(Builders<Post>.Filter.Lte(field, to.Value));
    }

    private static PostResponseDto ToResponse(Post post) => new(
        post.Id,
        post.PetId,
        post.AuthorUserId,
        post.Type,
        post.Caption,
        post.Media.Select(item => new PostMediaDto
        {
            Url = item.Url,
            PublicId = item.PublicId,
            Type = item.Type,
            Width = item.Width,
            Height = item.Height,
            DurationSeconds = item.DurationSeconds
        }).ToList(),
        post.Location,
        post.TaggedPetIds.Select(id => id.ToString()).ToList(),
        post.TaggedUserIds.Select(id => id.ToString()).ToList(),
        post.Hashtags,
        post.Visibility,
        post.LikesCount,
        post.CommentsCount,
        post.SharesCount,
        post.CreatedAt,
        post.UpdatedAt,
        post.DeletedAt);
}
