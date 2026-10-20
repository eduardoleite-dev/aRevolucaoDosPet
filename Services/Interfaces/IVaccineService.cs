using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IVaccineService
{
    Task<VaccineResponseDto> CreateAsync(
        CreateVaccineRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PagedResult<VaccineResponseDto>> SearchAsync(
        VaccineSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<VaccineResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<VaccineResponseDto?> UpdateAsync(
        string id,
        VaccineWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
