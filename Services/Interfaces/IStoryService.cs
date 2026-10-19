using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IStoryService
{
    Task<StoryResponseDto> CreateAsync(
        CreateStoryRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    // Retorna null quando o pet informado não está disponível para o usuário.
    Task<PagedResult<StoryResponseDto>?> SearchAsync(
        StorySearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<StoryResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<StoryResponseDto?> UpdateAsync(
        string id,
        UpdateStoryRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<StoryResponseDto?> RegisterViewAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}

public sealed class StoryPetNotFoundException : Exception;
public sealed class StoryAuthorNotFoundException : Exception;
public sealed class StoryPermissionException : Exception;
