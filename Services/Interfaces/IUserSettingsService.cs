using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IUserSettingsService
{
    Task<UserSettingsResponseDto> CreateAsync(string userId, UserSettingsRequest request, CancellationToken cancellationToken = default);
    Task<UserSettingsResponseDto?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<UserSettingsResponseDto?> UpdateAsync(string userId, UserSettingsRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
}

public sealed class UserSettingsAlreadyExistsException : Exception;
public sealed class SettingsUserNotFoundException : Exception;