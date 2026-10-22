using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class ExpenseService : IExpenseService
{
    private readonly IMongoCollection<Expense> _expenses;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;

    public ExpenseService(IMongoDatabase database)
    {
        _expenses = database.GetCollection<Expense>("Expenses");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
    }

    public async Task<ExpenseResponseDto> CreateAsync(
        CreateExpenseRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        await PetRecordHelper.RequireEditablePetAsync(_pets, request.PetId, currentUserId, isAdministrator, cancellationToken);
        var payerId = await ResolvePayerAsync(request.PayerUserId, currentUserId, isAdministrator, cancellationToken);

        var now = DateTime.UtcNow;
        var expense = new Expense
        {
            PetId = request.PetId,
            UserId = payerId,
            CreatedAt = now
        };
        Apply(expense, request, now);

        await _expenses.InsertOneAsync(expense, cancellationToken: cancellationToken);
        return ToResponse(expense);
    }

    public async Task<PagedResult<ExpenseResponseDto>> SearchAsync(
        ExpenseSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await PetRecordHelper.AuthorizeSearchAsync(_pets, filter.PetId, currentUserId, isAdministrator, cancellationToken);
        var filters = new List<FilterDefinition<Expense>>();
        var builder = Builders<Expense>.Filter;

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddObjectId(filters, "user_id", filter.UserId);
        PetRecordHelper.AddText(filters, "description", filter.Description);
        PetRecordHelper.AddText(filters, "vendor", filter.Vendor);
        PetRecordHelper.AddText(filters, "notes", filter.Notes);

        if (filter.Category.HasValue) filters.Add(builder.Eq(expense => expense.Category, filter.Category.Value));
        if (filter.MinAmount.HasValue) filters.Add(builder.Gte(expense => expense.Amount, filter.MinAmount.Value));
        if (filter.MaxAmount.HasValue) filters.Add(builder.Lte(expense => expense.Amount, filter.MaxAmount.Value));
        if (!string.IsNullOrWhiteSpace(filter.Currency)) filters.Add(builder.Eq(expense => expense.Currency, filter.Currency.Trim().ToUpperInvariant()));
        if (filter.IsRecurring.HasValue) filters.Add(builder.Eq(expense => expense.IsRecurring, filter.IsRecurring.Value));
        if (filter.RecurrenceFrequency.HasValue) filters.Add(builder.Eq("recurrence.frequency", filter.RecurrenceFrequency.Value.ToString()));
        if (filter.RecurrenceInterval.HasValue) filters.Add(builder.Eq("recurrence.interval", filter.RecurrenceInterval.Value));
        if (filter.HasReceipt.HasValue)
        {
            filters.Add(filter.HasReceipt.Value
                ? builder.Ne(expense => expense.ReceiptUrl, null)
                : builder.Eq(expense => expense.ReceiptUrl, null));
        }

        PetRecordHelper.AddDateRange(filters, "expense_date", filter.ExpenseDateFrom, filter.ExpenseDateTo);
        PetRecordHelper.AddDateRange(filters, "recurrence.next_date", filter.NextDateFrom, filter.NextDateTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _expenses.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var expenses = await _expenses.Find(query)
            .SortByDescending(expense => expense.ExpenseDate)
            .ThenBy(expense => expense.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ExpenseResponseDto>(
            expenses.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<ExpenseResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var expense = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return expense is null ? null : ToResponse(expense);
    }

    public async Task<ExpenseResponseDto?> UpdateAsync(
        string id,
        ExpenseWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var expense = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (expense is null) return null;

        if (!string.IsNullOrWhiteSpace(request.PayerUserId))
        {
            expense.UserId = await ResolvePayerAsync(request.PayerUserId, currentUserId, isAdministrator, cancellationToken);
        }

        Apply(expense, request, DateTime.UtcNow);
        await _expenses.ReplaceOneAsync(item => item.Id == id, expense, cancellationToken: cancellationToken);
        return ToResponse(expense);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var expense = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (expense is null) return false;
        var result = await _expenses.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<Expense?> FindAuthorizedAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var expense = await _expenses.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (expense is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, expense.PetId, currentUserId, isAdministrator, cancellationToken)
            ? expense
            : null;
    }

    private async Task<string> ResolvePayerAsync(
        string? requestedPayerId,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!isAdministrator && !string.IsNullOrWhiteSpace(requestedPayerId) && requestedPayerId != currentUserId)
        {
            throw new RecordPermissionException();
        }

        var payerId = isAdministrator && !string.IsNullOrWhiteSpace(requestedPayerId)
            ? requestedPayerId
            : currentUserId;
        PetRecordHelper.EnsureObjectId(payerId, "userId");
        var active = await _users.Find(user => user.Id == payerId && user.Status == Enums.UserStatus.ACTIVE)
            .AnyAsync(cancellationToken);
        if (!active) throw new ExpensePayerNotFoundException();
        return payerId!;
    }

    private static void Apply(Expense expense, ExpenseWriteRequest request, DateTime now)
    {
        var expenseDate = PetRecordHelper.NormalizeUtc(request.ExpenseDate);
        ExpenseRecurrence? recurrence = null;

        if (request.IsRecurring)
        {
            if (request.Recurrence is null) throw new ArgumentException("Gasto recorrente exige os dados de recorrência.");
            var nextDate = PetRecordHelper.NormalizeUtc(request.Recurrence.NextDate);
            if (nextDate <= expenseDate) throw new ArgumentException("A próxima data deve ser posterior à data do gasto.");
            recurrence = new ExpenseRecurrence
            {
                Frequency = request.Recurrence.Frequency,
                Interval = request.Recurrence.Interval,
                NextDate = nextDate
            };
        }
        else if (request.Recurrence is not null)
        {
            throw new ArgumentException("Informe recorrência somente para despesas recorrentes.");
        }

        expense.Category = request.Category;
        expense.Description = request.Description.Trim();
        expense.Amount = request.Amount;
        expense.Currency = request.Currency.Trim().ToUpperInvariant();
        expense.ExpenseDate = expenseDate;
        expense.Vendor = PetRecordHelper.NormalizeOptional(request.Vendor);
        expense.ReceiptUrl = PetRecordHelper.NormalizeOptional(request.ReceiptUrl);
        expense.IsRecurring = request.IsRecurring;
        expense.Recurrence = recurrence;
        expense.Notes = PetRecordHelper.NormalizeOptional(request.Notes);
        expense.UpdatedAt = now;
    }

    private static ExpenseResponseDto ToResponse(Expense expense) => new(
        expense.Id,
        expense.PetId,
        expense.UserId,
        expense.Category,
        expense.Description,
        expense.Amount,
        expense.Currency,
        expense.ExpenseDate,
        expense.Vendor,
        expense.ReceiptUrl,
        expense.IsRecurring,
        expense.Recurrence is null ? null : new ExpenseRecurrenceDto
        {
            Frequency = expense.Recurrence.Frequency,
            Interval = expense.Recurrence.Interval,
            NextDate = expense.Recurrence.NextDate
        },
        expense.Notes,
        expense.CreatedAt,
        expense.UpdatedAt);
}

public sealed class ExpensePayerNotFoundException : Exception;
