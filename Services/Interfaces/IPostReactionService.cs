using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IPostReactionService
{
    Task<PostReactionResult> ReactAsync(
        string postId,
        ReactToPostRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveReactionAsync(
        string postId,
        string? petId,
        string? currentUserId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PostReactionResponseDto>?> GetReactionsAsync(
        string postId,
        PostReactionSearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}

public sealed record PostReactionResult(PostReactionResponseDto Reaction, bool Created);

public sealed class PostNotAvailableException : Exception;
public sealed class ReactionPetNotFoundException : Exception;
public sealed class ReactionPermissionException : Exception;
