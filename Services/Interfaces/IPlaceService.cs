using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IPlaceService
{
    Task<PlaceResponseDto> CreateAsync(PlaceWriteRequest request, string? currentUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<PlaceResponseDto>> SearchAsync(PlaceSearchFilter filter, CancellationToken cancellationToken = default);
    Task<PlaceResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PlaceResponseDto?> UpdateAsync(string id, PlaceWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public interface IPlaceReviewService
{
    Task<PlaceReviewResponseDto> UpsertAsync(string placeId, PlaceReviewWriteRequest request, string? currentUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<PlaceReviewResponseDto>?> SearchAsync(string placeId, PlaceReviewSearchFilter filter, CancellationToken cancellationToken = default);
    Task<PlaceReviewResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PlaceReviewResponseDto?> UpdateAsync(string id, PlaceReviewWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public sealed class PlacePetNotFoundException : Exception;
public sealed class PlaceReviewUserNotFoundException : Exception;
public sealed class PlaceReviewPermissionException : Exception;
