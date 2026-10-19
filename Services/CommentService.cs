using System.Text.RegularExpressions;
using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace aRevolucaoDosPet.Services;

public class CommentService : ICommentService
{
    private readonly IMongoCollection<Comment> _comments;
    private readonly IMongoCollection<Post> _posts;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;
    private readonly IPostService _postService;

    public CommentService(IMongoDatabase database, IPostService postService)
    {
        _comments = database.GetCollection<Comment>("Comments");
        _posts = database.GetCollection<Post>("Posts");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
        _postService = postService;
    }

    public async Task<CommentResponseDto> CreateAsync(
        CreateCommentRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(currentUserId, out _))
        {
            throw new ArgumentException("Somente usuários cadastrados podem comentar.");
        }

        EnsureObjectId(request.PostId, "postId");
        var post = await _postService.GetByIdAsync(request.PostId, currentUserId, isAdministrator, cancellationToken);
        if (post is null || post.DeletedAt.HasValue) throw new CommentPostNotFoundException();

        var authorIsActive = await _users
            .Find(user => user.Id == currentUserId && user.Status == UserStatus.ACTIVE)
            .AnyAsync(cancellationToken);
        if (!authorIsActive) throw new CommentPermissionException();

        var petId = NormalizeOptional(request.PetId);
        if (petId is not null)
        {
            EnsureObjectId(petId, "petId");
            var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);
            if (pet is null) throw new CommentPetNotFoundException();
            if (!CanEdit(pet, currentUserId)) throw new CommentPermissionException();
        }

        var parentId = NormalizeOptional(request.ParentId);
        if (parentId is not null)
        {
            EnsureObjectId(parentId, "parentId");
            var parent = await _comments
                .Find(item => item.Id == parentId && item.PostId == request.PostId && item.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);
            if (parent is null) throw new CommentParentNotFoundException();
            if (parent.ParentId is not null)
            {
                throw new ArgumentException("Respostas só podem ser feitas a comentários de primeiro nível.");
            }
        }

        var now = DateTime.UtcNow;
        var comment = new Comment
        {
            PostId = request.PostId,
            PetId = petId,
            AuthorUserId = currentUserId!,
            ParentId = parentId,
            Content = request.Content.Trim(),
            Attachments = MapAttachments(request.Attachments),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _comments.InsertOneAsync(comment, cancellationToken: cancellationToken);
        await _posts.UpdateOneAsync(
            item => item.Id == request.PostId,
            Builders<Post>.Update.Inc(item => item.CommentsCount, 1),
            cancellationToken: cancellationToken);

        return ToResponse(comment);
    }

    public async Task<PagedResult<CommentResponseDto>?> SearchAsync(
        CommentSearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var hasPostId = !string.IsNullOrWhiteSpace(filter.PostId);
        if (!isAdministrator && !hasPostId) throw new ArgumentException("Informe o postId.");
        if (filter.RootOnly == true && !string.IsNullOrWhiteSpace(filter.ParentId))
        {
            throw new ArgumentException("rootOnly e parentId não podem ser usados juntos.");
        }

        if (hasPostId)
        {
            EnsureObjectId(filter.PostId, "postId");
            if (!isAdministrator)
            {
                var post = await _postService.GetByIdAsync(filter.PostId!, viewerUserId, false, cancellationToken);
                if (post is null || post.DeletedAt.HasValue) return null;
            }
        }

        var filters = new List<FilterDefinition<Comment>>();
        var builder = Builders<Comment>.Filter;

        AddTextFilter(filters, "content", filter.Content);
        AddObjectIdFilter(filters, "_id", filter.Id);
        AddObjectIdFilter(filters, "post_id", filter.PostId);
        AddObjectIdFilter(filters, "pet_id", filter.PetId);
        AddObjectIdFilter(filters, "author_user_id", filter.AuthorUserId);
        AddObjectIdFilter(filters, "parent_id", filter.ParentId);

        if (filter.RootOnly == true) filters.Add(builder.Eq(comment => comment.ParentId, null));
        if (filter.RootOnly == false) filters.Add(builder.Ne(comment => comment.ParentId, null));
        if (filter.HasAttachments.HasValue)
        {
            filters.Add(filter.HasAttachments.Value
                ? builder.SizeGt(comment => comment.Attachments, 0)
                : builder.Size(comment => comment.Attachments, 0));
        }

        if (!string.IsNullOrWhiteSpace(filter.Emoji))
        {
            var emoji = filter.Emoji.Trim();
            filters.Add(builder.ElemMatch(comment => comment.Reactions, reaction => reaction.Emoji == emoji));
        }

        AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);
        AddDateRange(filters, "deleted_at", filter.DeletedAtFrom, filter.DeletedAtTo);

        if (!(isAdministrator && filter.IncludeDeleted))
        {
            filters.Add(builder.Eq(comment => comment.DeletedAt, null));
        }

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _comments.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var comments = await _comments.Find(query)
            .SortByDescending(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentResponseDto>(
            comments.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<CommentResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var comment = await _comments.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (comment is null) return null;
        if (isAdministrator) return ToResponse(comment);
        if (comment.DeletedAt.HasValue) return null;

        if (comment.PostId is not null)
        {
            var post = await _postService.GetByIdAsync(comment.PostId, viewerUserId, false, cancellationToken);
            if (post is null || post.DeletedAt.HasValue) return null;
        }

        return ToResponse(comment);
    }

    public async Task<CommentResponseDto?> UpdateAsync(
        string id,
        CommentWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var existing = await _comments.Find(item => item.Id == id && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null) return null;
        if (!isAdministrator && existing.AuthorUserId != currentUserId) throw new CommentPermissionException();

        var update = Builders<Comment>.Update
            .Set(comment => comment.Content, request.Content.Trim())
            .Set(comment => comment.Attachments, MapAttachments(request.Attachments))
            .Set(comment => comment.UpdatedAt, DateTime.UtcNow);

        var updated = await _comments.FindOneAndUpdateAsync(
            comment => comment.Id == id && comment.DeletedAt == null,
            update,
            new FindOneAndUpdateOptions<Comment> { ReturnDocument = ReturnDocument.After },
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

        var existing = await _comments.Find(item => item.Id == id && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is null) return false;

        if (!isAdministrator &&
            existing.AuthorUserId != currentUserId &&
            !await IsPostPetEditorAsync(existing.PostId, currentUserId, cancellationToken))
        {
            throw new CommentPermissionException();
        }

        var now = DateTime.UtcNow;
        var builder = Builders<Comment>.Filter;
        var result = await _comments.UpdateManyAsync(
            builder.Eq(comment => comment.DeletedAt, null) &
            (builder.Eq(comment => comment.Id, id) | builder.Eq(comment => comment.ParentId, id)),
            Builders<Comment>.Update
                .Set(comment => comment.DeletedAt, now)
                .Set(comment => comment.UpdatedAt, now),
            cancellationToken: cancellationToken);
        if (result.ModifiedCount == 0) return false;

        if (existing.PostId is not null)
        {
            await _posts.UpdateOneAsync(
                post => post.Id == existing.PostId,
                Builders<Post>.Update.Inc(post => post.CommentsCount, -(int)result.ModifiedCount),
                cancellationToken: cancellationToken);
        }

        return true;
    }

    public async Task<CommentResponseDto?> ReactAsync(
        string id,
        ReactToCommentRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(currentUserId, out _))
        {
            throw new ArgumentException("Somente usuários cadastrados podem reagir a comentários.");
        }

        if (await GetByIdAsync(id, currentUserId, isAdministrator, cancellationToken) is not { DeletedAt: null })
        {
            return null;
        }

        var emoji = request.Emoji.Trim();
        var builder = Builders<Comment>.Filter;
        var reaction = new CommentReaction { UserId = currentUserId!, Emoji = emoji, CreatedAt = DateTime.UtcNow };

        var added = await _comments.UpdateOneAsync(
            builder.Eq(comment => comment.Id, id) &
            builder.Eq(comment => comment.DeletedAt, null) &
            builder.Not(builder.ElemMatch(comment => comment.Reactions, item => item.UserId == currentUserId)),
            Builders<Comment>.Update.Push(comment => comment.Reactions, reaction),
            cancellationToken: cancellationToken);

        if (added.ModifiedCount == 0)
        {
            await _comments.UpdateOneAsync(
                builder.Eq(comment => comment.Id, id) &
                builder.ElemMatch(comment => comment.Reactions, item => item.UserId == currentUserId),
                Builders<Comment>.Update.Set(comment => comment.Reactions.FirstMatchingElement().Emoji, emoji),
                cancellationToken: cancellationToken);
        }

        var updated = await _comments.Find(comment => comment.Id == id).FirstOrDefaultAsync(cancellationToken);
        return updated is null ? null : ToResponse(updated);
    }

    public async Task<bool> RemoveReactionAsync(
        string id,
        string? currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _) || !ObjectId.TryParse(currentUserId, out _)) return false;

        var result = await _comments.UpdateOneAsync(
            comment => comment.Id == id && comment.DeletedAt == null,
            Builders<Comment>.Update.PullFilter(comment => comment.Reactions, reaction => reaction.UserId == currentUserId),
            cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private async Task<bool> IsPostPetEditorAsync(string? postId, string? userId, CancellationToken cancellationToken)
    {
        if (postId is null || !ObjectId.TryParse(userId, out var userObjectId)) return false;

        var petId = await _posts.Find(post => post.Id == postId)
            .Project(post => post.PetId)
            .FirstOrDefaultAsync(cancellationToken);
        if (petId is null) return false;

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

    private static List<CommentAttachment> MapAttachments(IEnumerable<CommentAttachmentDto>? attachments) =>
        attachments?.Select(item => new CommentAttachment
        {
            Url = item.Url.Trim(),
            PublicId = NormalizeOptional(item.PublicId),
            Type = item.Type
        }).ToList() ?? [];

    private static void AddTextFilter(List<FilterDefinition<Comment>> filters, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            filters.Add(Builders<Comment>.Filter.Regex(field, new BsonRegularExpression(Regex.Escape(value.Trim()), "i")));
        }
    }

    private static void AddObjectIdFilter(List<FilterDefinition<Comment>> filters, string field, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!ObjectId.TryParse(value, out var objectId)) throw new ArgumentException($"O filtro {field} precisa ser um ObjectId válido.");
        filters.Add(new BsonDocumentFilterDefinition<Comment>(new BsonDocument(field, objectId)));
    }

    private static void AddDateRange(List<FilterDefinition<Comment>> filters, string field, DateTime? from, DateTime? to)
    {
        if (from.HasValue) filters.Add(Builders<Comment>.Filter.Gte(field, from.Value));
        if (to.HasValue) filters.Add(Builders<Comment>.Filter.Lte(field, to.Value));
    }

    private static CommentResponseDto ToResponse(Comment comment) => new(
        comment.Id,
        comment.PostId,
        comment.PetId,
        comment.AuthorUserId,
        comment.ParentId,
        comment.Content,
        comment.Attachments.Select(item => new CommentAttachmentDto
        {
            Url = item.Url,
            PublicId = item.PublicId,
            Type = item.Type
        }).ToList(),
        comment.Reactions.Select(item => new CommentReactionDto(item.UserId, item.Emoji, item.CreatedAt)).ToList(),
        comment.CreatedAt,
        comment.UpdatedAt,
        comment.DeletedAt);
}
