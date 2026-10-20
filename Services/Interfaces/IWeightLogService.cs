using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IWeightLogService
{
    Task<WeightLogResponseDto> CreateAsync(
        CreateWeightLogRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WeightLogResponseDto>> SearchAsync(
        WeightLogSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<WeightLogResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<WeightLogResponseDto?> UpdateAsync(
        string id,
        WeightLogWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
