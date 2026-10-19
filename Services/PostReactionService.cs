using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PostReactionService : IPostReactionService
{
    private readonly IMongoCollection<PostReaction> _reactions;
    private readonly IMongoCollection<Post> _posts;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IPostService _postService;

    public PostReactionService(IMongoDatabase database, IPostService postService)
    {
        _reactions = database.GetCollection<PostReaction>("PostReactions");
        _posts = database.GetCollection<Post>("Posts");
        _pets = database.GetCollection<Pet>("Pets");
        _postService = postService;
    }

    public async Task<PostReactionResult> ReactAsync(
        string postId,
        ReactToPostRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(postId, out _)) throw new ArgumentException("O postId informado não é um ObjectId válido.");
        if (!ObjectId.TryParse(currentUserId, out _))
        {
            throw new ArgumentException("Somente usuários cadastrados podem reagir a posts.");
        }

        var post = await _postService.GetByIdAsync(postId, currentUserId, isAdministrator, cancellationToken);
        if (post is null || post.DeletedAt.HasValue) throw new PostNotAvailableException();

        var petId = string.IsNullOrWhiteSpace(request.PetId) ? null : request.PetId.Trim();
        await ValidateReactingPetAsync(petId, request.Type, currentUserId!, cancellationToken);

        var filter = BuildReactionFilter(postId, currentUserId!, petId);
        var existing = await _reactions.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            if (existing.Type != request.Type)
            {
                await _reactions.UpdateOneAsync(
                    reaction => reaction.Id == existing.Id,
                    Builders<PostReaction>.Update.Set(reaction => reaction.Type, request.Type),
                    cancellationToken: cancellationToken);
                existing.Type = request.Type;
            }

            return new PostReactionResult(ToResponse(existing), false);
        }

        var reaction = new PostReaction
        {
            PostId = postId,
            UserId = currentUserId!,
            PetId = petId,
            Type = request.Type,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            await _reactions.InsertOneAsync(reaction, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            var concurrent = await _reactions.Find(filter).FirstAsync(cancellationToken);
            return new PostReactionResult(ToResponse(concurrent), false);
        }

        await _posts.UpdateOneAsync(
            item => item.Id == postId,
            Builders<Post>.Update.Inc(item => item.LikesCount, 1),
            cancellationToken: cancellationToken);
        return new PostReactionResult(ToResponse(reaction), true);
    }

    public async Task<bool> RemoveReactionAsync(
        string postId,
        string? petId,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedPetId = string.IsNullOrWhiteSpace(petId) ? null : petId.Trim();
        if (!ObjectId.TryParse(postId, out _) ||
            !ObjectId.TryParse(currentUserId, out _) ||
            (normalizedPetId is not null && !ObjectId.TryParse(normalizedPetId, out _)))
        {
            return false;
        }

        var result = await _reactions.DeleteOneAsync(
            BuildReactionFilter(postId, currentUserId!, normalizedPetId),
            cancellationToken);
        if (result.DeletedCount == 0) return false;

        await _posts.UpdateOneAsync(
            item => item.Id == postId,
            Builders<Post>.Update.Inc(item => item.LikesCount, -1),
            cancellationToken: cancellationToken);
        return true;
    }

    public async Task<PagedResult<PostReactionResponseDto>?> GetReactionsAsync(
        string postId,
        PostReactionSearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var post = await _postService.GetByIdAsync(postId, viewerUserId, isAdministrator, cancellationToken);
        if (post is null) return null;

        var builder = Builders<PostReaction>.Filter;
        var filters = new List<FilterDefinition<PostReaction>> { builder.Eq(reaction => reaction.PostId, postId) };
        if (filter.Type.HasValue) filters.Add(builder.Eq(reaction => reaction.Type, filter.Type.Value));
        AddObjectIdFilter(filters, "user_id", filter.UserId);
        AddObjectIdFilter(filters, "pet_id", filter.PetId);

        var query = builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _reactions.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var reactions = await _reactions.Find(query)
            .SortByDescending(reaction => reaction.CreatedAt)
            .ThenBy(reaction => reaction.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PostReactionResponseDto>(
            reactions.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    private async Task ValidateReactingPetAsync(
        string? petId,
        ReactionType type,
        string userId,
        CancellationToken cancellationToken)
    {
        if (petId is null)
        {
            if (type is ReactionType.BARK or ReactionType.MEOW)
            {
                throw new ArgumentException("As reações BARK e MEOW exigem reagir em nome de um pet.");
            }

            return;
        }

        if (!ObjectId.TryParse(petId, out _)) throw new ArgumentException("O petId informado não é um ObjectId válido.");

        var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new ReactionPetNotFoundException();

        var isEditor = pet.OwnerId == userId ||
                       (ObjectId.TryParse(userId, out var userObjectId) && pet.CoTutorIds.Contains(userObjectId));
        if (!isEditor) throw new ReactionPermissionException();

        if (type == ReactionType.BARK && pet.Species != Species.DOG)
        {
            throw new ArgumentException("A reação BARK é exclusiva para cães.");
        }

        if (type == ReactionType.MEOW && pet.Species != Species.CAT)
        {
            throw new ArgumentException("A reação MEOW é exclusiva para gatos.");
        }
    }

    private static FilterDefinition<PostReaction> BuildReactionFilter(string postId, string userId, string? petId)
    {
        var builder = Builders<PostReaction>.Filter;
        return builder.Eq(reaction => reaction.PostId, postId) &
               builder.Eq(reaction => reaction.UserId, userId) &
               builder.Eq(reaction => reaction.PetId, petId);
    }

    private static void AddObjectIdFilter(List<FilterDefinition<PostReaction>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<PostReaction>(new BsonDocument(field, objectId)));
    }

    private static PostReactionResponseDto ToResponse(PostReaction reaction) => new(
        reaction.Id,
        reaction.PostId,
        reaction.UserId,
        reaction.PetId,
        reaction.Type,
        reaction.CreatedAt);
}
