using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IPetFollowerService
{
    Task<PetFollowerResponseDto> FollowUserAsync(string petId, string userId, CancellationToken cancellationToken = default);
    Task<bool> UnfollowUserAsync(string petId, string userId, CancellationToken cancellationToken = default);
    Task<PetFollowerResponseDto> FollowPetAsync(
        string petId,
        string followerPetId,
        string? requesterUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
    Task<bool> UnfollowPetAsync(
        string petId,
        string followerPetId,
        string? requesterUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
    Task<PagedResult<PetFollowerResponseDto>> GetFollowersAsync(
        string petId,
        PetFollowerSearchFilter filter,
        CancellationToken cancellationToken = default);
}