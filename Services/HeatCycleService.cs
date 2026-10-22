using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class HeatCycleService : IHeatCycleService
{
    private const int MaxSymptoms = 30;
    private readonly IMongoCollection<HeatCycle> _heatCycles;
    private readonly IMongoCollection<Pet> _pets;

    public HeatCycleService(IMongoDatabase database)
    {
        _heatCycles = database.GetCollection<HeatCycle>("HeatCycles");
        _pets = database.GetCollection<Pet>("Pets");
    }

    public async Task<HeatCycleResponseDto> CreateAsync(
        CreateHeatCycleRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        var pet = await RequireFemalePetAsync(request.PetId, currentUserId, isAdministrator, cancellationToken);
        var now = DateTime.UtcNow;
        var cycle = new HeatCycle { PetId = pet.Id, CreatedAt = now };
        Apply(cycle, request, now);

        await _heatCycles.InsertOneAsync(cycle, cancellationToken: cancellationToken);
        return ToResponse(cycle);
    }

    public async Task<PagedResult<HeatCycleResponseDto>> SearchAsync(
        HeatCycleSearchFilter filter,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        await PetRecordHelper.AuthorizeSearchAsync(_pets, filter.PetId, currentUserId, isAdministrator, cancellationToken);
        var filters = new List<FilterDefinition<HeatCycle>>();
        var builder = Builders<HeatCycle>.Filter;

        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddText(filters, "notes", filter.Notes);
        PetRecordHelper.AddText(filters, "symptoms", filter.Symptom);

        if (filter.Phase.HasValue) filters.Add(builder.Eq(cycle => cycle.Phase, filter.Phase.Value));
        if (filter.IsActive.HasValue)
        {
            filters.Add(filter.IsActive.Value
                ? builder.Eq(cycle => cycle.EndedAt, null)
                : builder.Ne(cycle => cycle.EndedAt, null));
        }

        PetRecordHelper.AddDateRange(filters, "started_at", filter.StartedAtFrom, filter.StartedAtTo);
        PetRecordHelper.AddDateRange(filters, "ended_at", filter.EndedAtFrom, filter.EndedAtTo);
        PetRecordHelper.AddDateRange(filters, "predicted_next_at", filter.PredictedNextAtFrom, filter.PredictedNextAtTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _heatCycles.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var cycles = await _heatCycles.Find(query)
            .SortByDescending(cycle => cycle.StartedAt)
            .ThenBy(cycle => cycle.Id)
            .Skip(skip)
            .Limit(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HeatCycleResponseDto>(
            cycles.Select(ToResponse).ToList(),
            totalCount,
            filter.PageNumber,
            filter.PageSize);
    }

    public async Task<HeatCycleResponseDto?> GetByIdAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var cycle = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return cycle is null ? null : ToResponse(cycle);
    }

    public async Task<HeatCycleResponseDto?> UpdateAsync(
        string id,
        HeatCycleWriteRequest request,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var cycle = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (cycle is null) return null;
        await RequireFemalePetAsync(cycle.PetId, currentUserId, isAdministrator, cancellationToken);

        Apply(cycle, request, DateTime.UtcNow);
        await _heatCycles.ReplaceOneAsync(item => item.Id == id, cycle, cancellationToken: cancellationToken);
        return ToResponse(cycle);
    }

    public async Task<bool> DeleteAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken = default)
    {
        var cycle = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (cycle is null) return false;

        var result = await _heatCycles.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<Pet> RequireFemalePetAsync(
        string petId,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new HeatCyclePetNotFoundException();
        if (!isAdministrator && !PetRecordHelper.CanEdit(pet, currentUserId)) throw new RecordPermissionException();
        if (pet.Sex != Sex.FEMALE) throw new HeatCyclePetSexException();
        return pet;
    }

    private async Task<HeatCycle?> FindAuthorizedAsync(
        string id,
        string? currentUserId,
        bool isAdministrator,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var cycle = await _heatCycles.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (cycle is null) return null;

        return await PetRecordHelper.AuthorizeExistingAsync(_pets, cycle.PetId, currentUserId, isAdministrator, cancellationToken)
            ? cycle
            : null;
    }

    private static void Apply(HeatCycle cycle, HeatCycleWriteRequest request, DateTime now)
    {
        var startedAt = PetRecordHelper.NormalizeUtc(request.StartedAt);
        var endedAt = PetRecordHelper.NormalizeUtc(request.EndedAt);
        var predictedNextAt = PetRecordHelper.NormalizeUtc(request.PredictedNextAt);
        if (endedAt.HasValue && endedAt < startedAt) throw new ArgumentException("endedAt não pode ser anterior a startedAt.");
        if (predictedNextAt.HasValue && predictedNextAt < startedAt) throw new ArgumentException("A previsão deve ser posterior ao início do ciclo.");
        if (startedAt > now.AddDays(1)) throw new ArgumentException("startedAt não pode estar no futuro.");

        cycle.StartedAt = startedAt;
        cycle.EndedAt = endedAt;
        cycle.Phase = request.Phase;
        cycle.Symptoms = NormalizeSymptoms(request.Symptoms);
        cycle.Notes = PetRecordHelper.NormalizeOptional(request.Notes);
        cycle.PredictedNextAt = predictedNextAt;
        cycle.UpdatedAt = now;
    }

    private static List<string> NormalizeSymptoms(IEnumerable<string>? values)
    {
        var symptoms = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        if (symptoms.Any(symptom => symptom.Length > 100)) throw new ArgumentException("Cada sintoma aceita até 100 caracteres.");
        if (symptoms.Count > MaxSymptoms) throw new ArgumentException($"São permitidos até {MaxSymptoms} sintomas.");
        return symptoms;
    }

    private static HeatCycleResponseDto ToResponse(HeatCycle cycle) => new(
        cycle.Id,
        cycle.PetId,
        cycle.StartedAt,
        cycle.EndedAt,
        cycle.Phase,
        cycle.Symptoms,
        cycle.Notes,
        cycle.PredictedNextAt,
        cycle.CreatedAt,
        cycle.UpdatedAt);
}
