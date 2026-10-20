using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IDiaryEntryService
{
    Task<DiaryEntryResponseDto> CreateAsync(
        CreateDiaryEntryRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    // Retorna null quando o pet não existe ou o usuário não pode ver o perfil dele.
    Task<PagedResult<DiaryEntryResponseDto>?> SearchAsync(
        DiaryEntrySearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<DiaryEntryResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<DiaryEntryResponseDto?> UpdateAsync(
        string id,
        DiaryEntryWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}

public sealed class DiaryAuthorNotFoundException : Exception;
