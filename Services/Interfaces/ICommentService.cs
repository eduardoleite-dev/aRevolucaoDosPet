using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface ICommentService
{
    Task<CommentResponseDto> CreateAsync(
        CreateCommentRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    // Retorna null quando o post informado não está disponível para o usuário.
    Task<PagedResult<CommentResponseDto>?> SearchAsync(
        CommentSearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<CommentResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<CommentResponseDto?> UpdateAsync(
        string id,
        CommentWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<CommentResponseDto?> ReactAsync(
        string id,
        ReactToCommentRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveReactionAsync(string id, string? currentUserId, CancellationToken cancellationToken = default);
}

public sealed class CommentPostNotFoundException : Exception;
public sealed class CommentPetNotFoundException : Exception;
public sealed class CommentParentNotFoundException : Exception;
public sealed class CommentPermissionException : Exception;
