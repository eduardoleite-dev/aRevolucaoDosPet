using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IUserService
{
    Task<UserResponseDto?> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<UserResponseDto>> SearchAsync(UserSearchFilter filter, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<UserResponseDto?> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class DuplicateUserEmailException : Exception;