using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentResponseDto> CreateAsync(
        CreateAppointmentRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<PagedResult<AppointmentResponseDto>> SearchAsync(
        AppointmentSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<AppointmentResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<AppointmentResponseDto?> UpdateAsync(
        string id,
        AppointmentWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default);
}
