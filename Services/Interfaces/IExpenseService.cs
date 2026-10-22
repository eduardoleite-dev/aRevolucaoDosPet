using aRevolucaoDosPet.DTOs;

namespace aRevolucaoDosPet.Services.Interfaces;

public interface IExpenseService
{
    Task<ExpenseResponseDto> CreateAsync(CreateExpenseRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<PagedResult<ExpenseResponseDto>> SearchAsync(ExpenseSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<ExpenseResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<ExpenseResponseDto?> UpdateAsync(string id, ExpenseWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default);
}
