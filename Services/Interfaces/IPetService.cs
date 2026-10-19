using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IPetService
{
    Task<PetResponseDto> CreateAsync(
        CreatePetRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PetResponseDto>> SearchAsync(
        PetSearchFilter filter,
        CancellationToken cancellationToken = default);

    Task<PetResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PetResponseDto?> UpdateAsync(
        string id,
        PetWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> CanEditAsync(string id, string? userId, bool isAdministrator, bool ownerOnly = false, CancellationToken cancellationToken = default);
}

public sealed class DuplicatePetUsernameException : Exception;
public sealed class PetPermissionException : Exception;
public sealed class PetOwnerNotFoundException : Exception;
public sealed class PetNotFoundException : Exception;
public sealed class PetFollowerNotFoundException : Exception;
public sealed class PetAlreadyFollowedException : Exception;
public sealed class PetFollowerPermissionException : Exception;