using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IPostService
{
    Task<PostResponseDto> CreateAsync(
        CreatePostRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PostResponseDto>> SearchAsync(
        PostSearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PostResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PostResponseDto?> UpdateAsync(
        string id,
        PostWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}

public sealed class PostPetNotFoundException : Exception;
public sealed class PostAuthorNotFoundException : Exception;
public sealed class PostPermissionException : Exception;
