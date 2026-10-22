using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationResponseDto> CreateAsync(CreateNotificationRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<NotificationResponseDto>> SearchAsync(NotificationSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<NotificationResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> MarkReadAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}

public sealed class NotificationUserNotFoundException : Exception;
public sealed class NotificationPetNotFoundException : Exception;
