using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IHeatCycleService
{
    Task<HeatCycleResponseDto> CreateAsync(CreateHeatCycleRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PagedResult<HeatCycleResponseDto>> SearchAsync(HeatCycleSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<HeatCycleResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<HeatCycleResponseDto?> UpdateAsync(string id, HeatCycleWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public sealed class HeatCyclePetNotFoundException : Exception;
public sealed class HeatCyclePetSexException : Exception;
