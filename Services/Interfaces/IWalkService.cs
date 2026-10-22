using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IWalkService
{
    Task<WalkResponseDto> CreateAsync(CreateWalkRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PagedResult<WalkResponseDto>> SearchAsync(WalkSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<WalkResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<WalkResponseDto?> UpdateAsync(string id, WalkWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public interface IWalkEncounterService
{
    Task<WalkEncounterResponseDto> CreateAsync(string walkId, WalkEncounterWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PagedResult<WalkEncounterResponseDto>> SearchAsync(string walkId, WalkEncounterSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<WalkEncounterResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<WalkEncounterResponseDto?> UpdateAsync(string id, WalkEncounterWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public sealed class WalkPetNotFoundException : Exception;
public sealed class WalkUserNotFoundException : Exception;
public sealed class WalkPermissionException : Exception;
public sealed class WalkEncounterNotFoundException : Exception;
