using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class WeightLogService : IWeightLogService
{
    private readonly IMongoCollection<WeightLog> _weightLogs;
    private readonly IMongoCollection<Pet> _pets;

    public WeightLogService(IMongoDatabase database)
    {
        _weightLogs = database.GetCollection<WeightLog>("WeightLogs");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<WeightLogResponseDto> CreateAsync(
        CreateWeightLogRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        await PetRecordHelper.RequireEditablePetAsync(_pets, request.PetId, currentUserId, isAdministrator, cancellationToken);

        var weightLog = new WeightLog { PetId = request.PetId, CreatedAt = DateTime.UtcNow };
        Apply(weightLog, request);

        await _weightLogs.InsertOneAsync(weightLog, cancellationToken: cancellationToken);
        return ToResponse(weightLog);
    }

    public async Task<PagedResult<WeightLogResponseDto>> SearchAsync(
        WeightLogSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await PetRecordHelper.AuthorizeSearchAsync(_pets, filter.PetId, currentUserId, isAdministrator, cancellationToken);

        var filters = new List<FilterDefinition<WeightLog>>();
        var builder = Builders<WeightLog>.Filter;

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddText(filters, "notes", filter.Notes);

        if (filter.MinWeightKg.HasValue) filters.Add(builder.Gte(log => log.WeightKg, filter.MinWeightKg.Value));
        if (filter.MaxWeightKg.HasValue) filters.Add(builder.Lte(log => log.WeightKg, filter.MaxWeightKg.Value));
        if (filter.MinBodyScore.HasValue) filters.Add(builder.Gte(log => log.BodyScore, filter.MinBodyScore.Value));
        if (filter.MaxBodyScore.HasValue) filters.Add(builder.Lte(log => log.BodyScore, filter.MaxBodyScore.Value));

        PetRecordHelper.AddDateRange(filters, "measured_at", filter.MeasuredAtFrom, filter.MeasuredAtTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _weightLogs.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var logs = await _weightLogs.Find(query)
            .SortByDescending(log => log.MeasuredAt)
            .ThenBy(log => log.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<WeightLogResponseDto>(
            logs.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<WeightLogResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var weightLog = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return weightLog is null ? null : ToResponse(weightLog);
    }

    public async Task<WeightLogResponseDto?> UpdateAsync(
        string id,
        WeightLogWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var weightLog = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (weightLog is null) return null;

        Apply(weightLog, request);
        await _weightLogs.ReplaceOneAsync(item => item.Id == id, weightLog, cancellationToken: cancellationToken);
        return ToResponse(weightLog);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var weightLog = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (weightLog is null) return false;

        var result = await _weightLogs.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<WeightLog?> FindAuthorizedAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;

        var weightLog = await _weightLogs.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (weightLog is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, weightLog.PetId, currentUserId, isAdministrator, cancellationToken)
            ? weightLog
            : null;
    }

    private static void Apply(WeightLog weightLog, WeightLogWriteRequest request)
    {
        var measuredAt = PetRecordHelper.NormalizeUtc(request.MeasuredAt);

        // Tolerância de um dia para diferenças de fuso do cliente.
        if (measuredAt > DateTime.UtcNow.AddDays(1)) throw new ArgumentException("A data da medição não pode estar no futuro.");

        weightLog.WeightKg = request.WeightKg;
        weightLog.BodyScore = request.BodyScore;
        weightLog.MeasuredAt = measuredAt;
        weightLog.Notes = PetRecordHelper.NormalizeOptional(request.Notes);
    }

    private static WeightLogResponseDto ToResponse(WeightLog weightLog) => new(
        weightLog.Id,
        weightLog.PetId,
        weightLog.WeightKg,
        weightLog.BodyScore,
        weightLog.MeasuredAt,
        weightLog.Notes,
        weightLog.CreatedAt);
}
