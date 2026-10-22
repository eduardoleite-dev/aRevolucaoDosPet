using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class WalkEncounterService : IWalkEncounterService
{
    private readonly IMongoCollection<WalkEncounter> _encounters;
    private readonly IMongoCollection<Walk> _walks;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IWalkService _walkService;

    public WalkEncounterService(IMongoDatabase database, IWalkService walkService)
    {
        _encounters = database.GetCollection<WalkEncounter>("WalkEncounters");
        _walks = database.GetCollection<Walk>("Walks");
        _pets = database.GetCollection<Pet>("Pets");
        _walkService = walkService;
    }

    public async Task<WalkEncounterResponseDto> CreateAsync(string walkId, WalkEncounterWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var walk = await RequireWalkAsync(walkId, currentUserId, isAdministrator, cancellationToken);
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        if (request.PetId != walk.PetId) throw new ArgumentException("petId deve ser o pet associado ao passeio.");

        var otherPetId = PetRecordHelper.NormalizeOptional(request.OtherPetId);
        var otherPetName = PetRecordHelper.NormalizeOptional(request.OtherPetName);
        if (otherPetId is not null && otherPetName is not null) throw new ArgumentException("Informe otherPetId ou otherPetName, não ambos.");
        if (otherPetId is not null)
        {
            PetRecordHelper.EnsureObjectId(otherPetId, "otherPetId");
            var exists = await _pets.Find(pet => pet.Id == otherPetId && pet.DeletedAt == null).AnyAsync(cancellationToken);
            if (!exists) throw new WalkPetNotFoundException();
        }

        var encounter = new WalkEncounter
        {
            WalkId = walk.Id,
            PetId = walk.PetId,
            OtherPetId = otherPetId,
            OtherPetName = otherPetName,
            Location = GeoJsonMapper.MapPoint(request.Location, "location"),
            InteractionType = request.InteractionType,
            Notes = PetRecordHelper.NormalizeOptional(request.Notes),
            CreatedAt = DateTime.UtcNow
        };
        await _encounters.InsertOneAsync(encounter, cancellationToken: cancellationToken);
        return ToResponse(encounter);
    }

    public async Task<PagedResult<WalkEncounterResponseDto>> SearchAsync(string walkId, WalkEncounterSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        await RequireWalkAsync(walkId, currentUserId, isAdministrator, cancellationToken);
        var filters = new List<FilterDefinition<WalkEncounter>> { Builders<WalkEncounter>.Filter.Eq(item => item.WalkId, walkId) };
        var builder = Builders<WalkEncounter>.Filter;
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddObjectId(filters, "other_pet_id", filter.OtherPetId);
        if (filter.InteractionType.HasValue) filters.Add(builder.Eq(item => item.InteractionType, filter.InteractionType.Value));
        if (filter.HasOtherPetId.HasValue)
        {
            filters.Add(filter.HasOtherPetId.Value
                ? builder.Ne(item => item.OtherPetId, null)
                : builder.Eq(item => item.OtherPetId, null));
        }
        if (filter.HasLocation.HasValue)
        {
            filters.Add(filter.HasLocation.Value
                ? builder.Ne(item => item.Location, null)
                : builder.Eq(item => item.Location, null));
        }

        WalkService.ValidateGeoFilter(filter.Longitude, filter.Latitude, filter.RadiusMeters);
        if (filter.RadiusMeters.HasValue)
        {
            WalkService.AddRadiusFilter(filters, "location", filter.Longitude!.Value, filter.Latitude!.Value, filter.RadiusMeters.Value);
        }
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);

        var query = builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var count = await _encounters.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var items = await _encounters.Find(query).SortByDescending(item => item.CreatedAt).ThenBy(item => item.Id)
            .Skip(skip).Limit(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<WalkEncounterResponseDto>(items.Select(ToResponse).ToList(), count, filter.PageNumber, filter.PageSize);
    }

    public async Task<WalkEncounterResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var encounter = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return encounter is null ? null : ToResponse(encounter);
    }

    public async Task<WalkEncounterResponseDto?> UpdateAsync(string id, WalkEncounterWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var encounter = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (encounter is null) return null;
        var walk = await RequireWalkAsync(encounter.WalkId, currentUserId, isAdministrator, cancellationToken);
        if (request.PetId != walk.PetId) throw new ArgumentException("petId deve ser o pet associado ao passeio.");

        var otherPetId = PetRecordHelper.NormalizeOptional(request.OtherPetId);
        var otherPetName = PetRecordHelper.NormalizeOptional(request.OtherPetName);
        if (otherPetId is not null && otherPetName is not null) throw new ArgumentException("Informe otherPetId ou otherPetName, não ambos.");
        if (otherPetId is not null)
        {
            PetRecordHelper.EnsureObjectId(otherPetId, "otherPetId");
            if (!await _pets.Find(pet => pet.Id == otherPetId && pet.DeletedAt == null).AnyAsync(cancellationToken)) throw new WalkPetNotFoundException();
        }

        encounter.OtherPetId = otherPetId;
        encounter.OtherPetName = otherPetName;
        encounter.Location = GeoJsonMapper.MapPoint(request.Location, "location");
        encounter.InteractionType = request.InteractionType;
        encounter.Notes = PetRecordHelper.NormalizeOptional(request.Notes);
        await _encounters.ReplaceOneAsync(item => item.Id == id, encounter, cancellationToken: cancellationToken);
        return ToResponse(encounter);
    }

    public async Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var encounter = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (encounter is null) return false;
        return (await _encounters.DeleteOneAsync(item => item.Id == id, cancellationToken)).DeletedCount == 1;
    }

    private async Task<Walk> RequireWalkAsync(string walkId, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(walkId, out _)) throw new WalkEncounterNotFoundException();
        var walk = await _walks.Find(item => item.Id == walkId).FirstOrDefaultAsync(cancellationToken);
        if (walk is null) throw new WalkEncounterNotFoundException();
        if (isAdministrator || walk.UserId == currentUserId) return walk;

        var pet = await _pets.Find(item => item.Id == walk.PetId && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new WalkPetNotFoundException();
        if (!PetRecordHelper.CanEdit(pet, currentUserId)) throw new WalkPermissionException();
        return walk;
    }

    private async Task<WalkEncounter?> FindAuthorizedAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var encounter = await _encounters.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (encounter is null) return null;
        await RequireWalkAsync(encounter.WalkId, currentUserId, isAdministrator, cancellationToken);
        return encounter;
    }

    private static WalkEncounterResponseDto ToResponse(WalkEncounter item) => new(
        item.Id, item.WalkId, item.PetId, item.OtherPetId, item.OtherPetName,
        GeoJsonMapper.ToPointDto(item.Location), item.InteractionType, item.Notes, item.CreatedAt);
}
