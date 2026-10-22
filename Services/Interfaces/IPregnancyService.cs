using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IPregnancyService
{
    Task<PregnancyResponseDto> CreateAsync(CreatePregnancyRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PagedResult<PregnancyResponseDto>> SearchAsync(PregnancySearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PregnancyResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PregnancyResponseDto?> UpdateAsync(string id, PregnancyWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public sealed class PregnancyPetNotFoundException : Exception;
public sealed class PregnancyPetSexException : Exception;
