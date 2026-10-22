using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class DiaryEntryService : IDiaryEntryService
{
    private const int MaxListItems = 30;
    private const int MaxTermLength = 50;

    private readonly IMongoCollection<DiaryEntry> _entries;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<PetFollower> _petFollowers;
    private readonly IMongoCollection<Expense> _expenses;

    public DiaryEntryService(IMongoDatabase database)
    {
        _entries = database.GetCollection<DiaryEntry>("DiaryEntries");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
        _petFollowers = database.GetCollection<PetFollower>("PetFollowers");
        _expenses = database.GetCollection<Expense>("Expenses");
    }

    public async Task<DiaryEntryResponseDto> CreateAsync(
        CreateDiaryEntryRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var authorUserId = isAdministrator && !string.IsNullOrWhiteSpace(request.AuthorUserId)
            ? request.AuthorUserId
            : currentUserId;
        PetRecordHelper.EnsureObjectId(authorUserId, "authorUserId");
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        await PetRecordHelper.RequireEditablePetAsync(_pets, request.PetId, currentUserId, isAdministrator, cancellationToken);

        var authorExists = await _users.Find(user => user.Id == authorUserId && user.Status == UserStatus.ACTIVE)
            .AnyAsync(cancellationToken);
        if (!authorExists) throw new DiaryAuthorNotFoundException();
        await ValidateRelatedExpenseAsync(request.RelatedExpenseId, request.PetId, cancellationToken);

        var now = DateTime.UtcNow;
        var entry = new DiaryEntry { PetId = request.PetId, AuthorUserId = authorUserId!, CreatedAt = now };
        Apply(entry, request, now);

        await _entries.InsertOneAsync(entry, cancellationToken: cancellationToken);
        return ToResponse(entry);
    }

    public async Task<PagedResult<DiaryEntryResponseDto>?> SearchAsync(
        DiaryEntrySearchFilter filter,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var hasPetId = !string.IsNullOrWhiteSpace(filter.PetId);
        if (!isAdministrator && !hasPetId) throw new ArgumentException("Informe o petId.");
        if (hasPetId) PetRecordHelper.EnsureObjectId(filter.PetId, "petId");

        var filters = new List<FilterDefinition<DiaryEntry>>();
        var builder = Builders<DiaryEntry>.Filter;

        if (!isAdministrator)
        {
            var pet = await _pets.Find(item => item.Id == filter.PetId && item.DeletedAt == null)
                .FirstOrDefaultAsync(cancellationToken);
            if (pet is null) return null;

            if (!PetRecordHelper.CanEdit(pet, viewerUserId))
            {
                if (!await PetRecordHelper.CanViewProfileAsync(pet, _petFollowers, viewerUserId, cancellationToken))
                {
                    return null;
                }

                filters.Add(builder.Eq(entry => entry.IsPrivate, false));
            }
        }

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddObjectId(filters, "author_user_id", filter.AuthorUserId);
        PetRecordHelper.AddObjectId(filters, "related_expense_id", filter.RelatedExpenseId);
        PetRecordHelper.AddText(filters, "title", filter.Title);
        PetRecordHelper.AddText(filters, "content", filter.Content);

        if (filter.Mood.HasValue) filters.Add(builder.Eq(entry => entry.Mood, filter.Mood.Value));
        if (filter.Appetite.HasValue) filters.Add(builder.Eq(entry => entry.Appetite, filter.Appetite.Value));
        if (filter.Energy.HasValue) filters.Add(builder.Eq(entry => entry.Energy, filter.Energy.Value));
        if (filter.Stool.HasValue) filters.Add(builder.Eq(entry => entry.Stool, filter.Stool.Value));
        if (filter.Urine.HasValue) filters.Add(builder.Eq(entry => entry.Urine, filter.Urine.Value));
        if (filter.IsPrivate.HasValue) filters.Add(builder.Eq(entry => entry.IsPrivate, filter.IsPrivate.Value));

        if (!string.IsNullOrWhiteSpace(filter.Symptom))
        {
            filters.Add(builder.Eq("symptoms", filter.Symptom.Trim().ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag))
        {
            filters.Add(builder.Eq("tags", filter.Tag.Trim().ToLowerInvariant()));
        }

        if (filter.HasMedia.HasValue)
        {
            filters.Add(filter.HasMedia.Value
                ? builder.SizeGt(entry => entry.Media, 0)
                : builder.Size(entry => entry.Media, 0));
        }

        if (filter.HasRelatedExpense.HasValue)
        {
            filters.Add(filter.HasRelatedExpense.Value
                ? builder.Ne(entry => entry.RelatedExpenseId, null)
                : builder.Eq(entry => entry.RelatedExpenseId, null));
        }

        PetRecordHelper.AddDateRange(filters, "entry_date", filter.EntryDateFrom, filter.EntryDateTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _entries.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var entries = await _entries.Find(query)
            .SortByDescending(entry => entry.EntryDate)
            .ThenBy(entry => entry.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DiaryEntryResponseDto>(
            entries.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<DiaryEntryResponseDto?> GetByIdAsync(
        string id,
        string? viewerUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var entry = await _entries.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (entry is null) return null;

        var pet = await _pets.Find(item => item.Id == entry.PetId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) return null;

        if (isAdministrator || PetRecordHelper.CanEdit(pet, viewerUserId)) return ToResponse(entry);
        if (entry.IsPrivate) return null;

        return await PetRecordHelper.CanViewProfileAsync(pet, _petFollowers, viewerUserId, cancellationToken)
            ? ToResponse(entry)
            : null;
    }

    public async Task<DiaryEntryResponseDto?> UpdateAsync(
        string id,
        DiaryEntryWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var entry = await FindEditableAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (entry is null) return null;

        await ValidateRelatedExpenseAsync(request.RelatedExpenseId, entry.PetId, cancellationToken);
        Apply(entry, request, DateTime.UtcNow);
        await _entries.ReplaceOneAsync(item => item.Id == id, entry, cancellationToken: cancellationToken);
        return ToResponse(entry);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var entry = await FindEditableAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (entry is null) return false;

        var result = await _entries.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<DiaryEntry?> FindEditableAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var entry = await _entries.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (entry is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, entry.PetId, currentUserId, isAdministrator, cancellationToken)
            ? entry
            : null;
    }

    private static void Apply(DiaryEntry entry, DiaryEntryWriteRequest request, DateTime now)
    {
        var symptoms = NormalizeTerms(request.Symptoms, "symptoms");
        var tags = NormalizeTerms(request.Tags, "tags");
        var title = PetRecordHelper.NormalizeOptional(request.Title);
        var content = PetRecordHelper.NormalizeOptional(request.Content);

        var hasContent = title is not null || content is not null || symptoms.Count > 0 || request.Media.Count > 0 ||
                         request.Mood.HasValue || request.Appetite.HasValue || request.Energy.HasValue ||
                         request.Stool.HasValue || request.Urine.HasValue;
        if (!hasContent) throw new ArgumentException("Informe ao menos um dado na entrada do diário.");

        var relatedExpenseId = PetRecordHelper.NormalizeOptional(request.RelatedExpenseId);
        if (relatedExpenseId is not null) PetRecordHelper.EnsureObjectId(relatedExpenseId, "relatedExpenseId");

        if (request.EntryDate.HasValue)
        {
            var entryDate = PetRecordHelper.NormalizeUtc(request.EntryDate.Value);

            // Tolerância de um dia para diferenças de fuso do cliente.
            if (entryDate > now.AddDays(1)) throw new ArgumentException("A data da entrada não pode estar no futuro.");
            entry.EntryDate = entryDate;
        }

        entry.Title = title;
        entry.Content = content;
        entry.Mood = request.Mood;
        entry.Appetite = request.Appetite;
        entry.Energy = request.Energy;
        entry.Stool = request.Stool;
        entry.Urine = request.Urine;
        entry.Symptoms = symptoms;
        entry.Media = request.Media.Select(item => new DiaryMedia
        {
            Url = item.Url.Trim(),
            PublicId = PetRecordHelper.NormalizeOptional(item.PublicId),
            Type = item.Type
        }).ToList();
        entry.Tags = tags;
        entry.IsPrivate = request.IsPrivate;
        entry.RelatedExpenseId = relatedExpenseId;
        entry.UpdatedAt = now;
    }

    private static List<string> NormalizeTerms(IEnumerable<string>? values, string field)
    {
        if (values is null) return [];

        var result = new List<string>();
        foreach (var value in values)
        {
            var term = value?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(term)) continue;
            if (term.Length > MaxTermLength)
            {
                throw new ArgumentException($"Cada item de {field} aceita no máximo {MaxTermLength} caracteres.");
            }

            if (!result.Contains(term)) result.Add(term);
        }

        if (result.Count > MaxListItems) throw new ArgumentException($"{field} aceita no máximo {MaxListItems} itens.");
        return result;
    }

    private async Task ValidateRelatedExpenseAsync(
        string? relatedExpenseId,
        string petId,
        CancellationToken cancellationToken)
    {
        var expenseId = PetRecordHelper.NormalizeOptional(relatedExpenseId);
        if (expenseId is null) return;
        PetRecordHelper.EnsureObjectId(expenseId, "relatedExpenseId");

        var existsForPet = await _expenses
            .Find(expense => expense.Id == expenseId && expense.PetId == petId)
            .AnyAsync(cancellationToken);
        if (!existsForPet)
        {
            throw new ArgumentException("O gasto relacionado não existe ou pertence a outro pet.");
        }
    }

    private static DiaryEntryResponseDto ToResponse(DiaryEntry entry) => new(
        entry.Id,
        entry.PetId,
        entry.AuthorUserId,
        entry.Title,
        entry.Content,
        entry.Mood,
        entry.Appetite,
        entry.Energy,
        entry.Stool,
        entry.Urine,
        entry.Symptoms,
        entry.Media.Select(item => new DiaryMediaDto { Url = item.Url, PublicId = item.PublicId, Type = item.Type }).ToList(),
        entry.Tags,
        entry.IsPrivate,
        entry.RelatedExpenseId,
        entry.EntryDate,
        entry.CreatedAt,
        entry.UpdatedAt);
}
