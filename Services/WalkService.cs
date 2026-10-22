using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class WalkService : IWalkService
{
    private readonly IMongoCollection<Walk> _walks;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;
    private readonly IMongoCollection<WalkEncounter> _encounters;

    public WalkService(IMongoDatabase database)
    {
        _walks = database.GetCollection<Walk>("Walks");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
        _encounters = database.GetCollection<WalkEncounter>("WalkEncounters");
    }

    public async Task<WalkResponseDto> CreateAsync(CreateWalkRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(request.PetId, "petId");
        var pet = await _pets.Find(item => item.Id == request.PetId && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new WalkPetNotFoundException();
        if (!isAdministrator && !PetRecordHelper.CanEdit(pet, currentUserId)) throw new WalkPermissionException();

        var userId = isAdministrator && !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : currentUserId;
        PetRecordHelper.EnsureObjectId(userId, "userId");
        var activeUser = await _users.Find(user => user.Id == userId && user.Status == UserStatus.ACTIVE).AnyAsync(cancellationToken);
        if (!activeUser) throw new WalkUserNotFoundException();

        var now = DateTime.UtcNow;
        var walk = new Walk { PetId = request.PetId, UserId = userId!, CreatedAt = now };
        Apply(walk, request, now);
        await _walks.InsertOneAsync(walk, cancellationToken: cancellationToken);
        return ToResponse(walk);
    }

    public async Task<PagedResult<WalkResponseDto>> SearchAsync(WalkSearchFilter filter, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Walk>>();
        var builder = Builders<Walk>.Filter;
        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        PetRecordHelper.AddObjectId(filters, "user_id", filter.UserId);

        if (!isAdministrator)
        {
            if (!ObjectId.TryParse(currentUserId, out _)) throw new ArgumentException("Usuário autenticado inválido.");
            if (string.IsNullOrWhiteSpace(filter.PetId)) throw new ArgumentException("Informe petId para consultar caminhadas.");
            var pet = await _pets.Find(item => item.Id == filter.PetId && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
            if (pet is null) throw new WalkPetNotFoundException();
            if (PetRecordHelper.CanEdit(pet, currentUserId))
            {
                if (!string.IsNullOrWhiteSpace(filter.UserId) && filter.UserId != currentUserId)
                {
                    filters.Add(builder.Eq(walk => walk.PetId, filter.PetId));
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(filter.UserId) && filter.UserId != currentUserId) throw new WalkPermissionException();
                filters.Add(builder.Eq(walk => walk.UserId, currentUserId));
            }
        }

        if (filter.Status.HasValue) filters.Add(builder.Eq(walk => walk.Status, filter.Status.Value));
        if (filter.MinDurationSeconds.HasValue) filters.Add(builder.Gte(walk => walk.DurationSeconds, filter.MinDurationSeconds.Value));
        if (filter.MaxDurationSeconds.HasValue) filters.Add(builder.Lte(walk => walk.DurationSeconds, filter.MaxDurationSeconds.Value));
        if (filter.MinDistanceMeters.HasValue) filters.Add(builder.Gte(walk => walk.DistanceMeters, filter.MinDistanceMeters.Value));
        if (filter.MaxDistanceMeters.HasValue) filters.Add(builder.Lte(walk => walk.DistanceMeters, filter.MaxDistanceMeters.Value));
        if (filter.MinAverageSpeedKmh.HasValue) filters.Add(builder.Gte(walk => walk.AverageSpeedKmh, filter.MinAverageSpeedKmh.Value));
        if (filter.MaxAverageSpeedKmh.HasValue) filters.Add(builder.Lte(walk => walk.AverageSpeedKmh, filter.MaxAverageSpeedKmh.Value));
        AddHasFilter(filters, "route", filter.HasRoute);
        AddHasFilter(filters, "start_location", filter.HasStartLocation);
        AddHasFilter(filters, "end_location", filter.HasEndLocation);
        AddArrayFilter(filters, "photos", filter.HasPhotos);
        PetRecordHelper.AddText(filters, "weather.condition", filter.WeatherCondition);
        if (filter.MinTemperature.HasValue) filters.Add(builder.Gte("weather.temperature", filter.MinTemperature.Value));
        if (filter.MaxTemperature.HasValue) filters.Add(builder.Lte("weather.temperature", filter.MaxTemperature.Value));

        ValidateGeoFilter(filter.Longitude, filter.Latitude, filter.RadiusMeters);
        if (filter.RadiusMeters.HasValue)
        {
            AddRadiusFilter(filters, "route", filter.Longitude!.Value, filter.Latitude!.Value, filter.RadiusMeters.Value);
        }
        else if (filter.Longitude.HasValue)
        {
            filters.Add(builder.Eq("start_location.coordinates.0", filter.Longitude.Value));
            filters.Add(builder.Eq("start_location.coordinates.1", filter.Latitude!.Value));
        }

        PetRecordHelper.AddDateRange(filters, "started_at", filter.StartedAtFrom, filter.StartedAtTo);
        PetRecordHelper.AddDateRange(filters, "ended_at", filter.EndedAtFrom, filter.EndedAtTo);
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);
        PetRecordHelper.AddDateRange(filters, "updated_at", filter.UpdatedAtFrom, filter.UpdatedAtTo);

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var totalCount = await _walks.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var items = await _walks.Find(query).SortByDescending(walk => walk.StartedAt).ThenBy(walk => walk.Id)
            .Skip(skip).Limit(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<WalkResponseDto>(items.Select(ToResponse).ToList(), totalCount, filter.PageNumber, filter.PageSize);
    }

    public async Task<WalkResponseDto?> GetByIdAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var walk = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        return walk is null ? null : ToResponse(walk);
    }

    public async Task<WalkResponseDto?> UpdateAsync(string id, WalkWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var walk = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (walk is null) return null;
        Apply(walk, request, DateTime.UtcNow);
        await _walks.ReplaceOneAsync(item => item.Id == id, walk, cancellationToken: cancellationToken);
        return ToResponse(walk);
    }

    public async Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var walk = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (walk is null) return false;
        await _encounters.DeleteManyAsync(encounter => encounter.WalkId == id, cancellationToken);
        var result = await _walks.DeleteOneAsync(item => item.Id == id, cancellationToken);
        return result.DeletedCount == 1;
    }

    private async Task<Walk?> FindAuthorizedAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var walk = await _walks.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (walk is null) return null;
        if (isAdministrator || walk.UserId == currentUserId) return walk;
        var pet = await _pets.Find(item => item.Id == walk.PetId && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
        if (pet is null) return null;
        if (!PetRecordHelper.CanEdit(pet, currentUserId)) throw new WalkPermissionException();
        return walk;
    }

    private static void Apply(Walk walk, WalkWriteRequest request, DateTime now)
    {
        var startedAt = PetRecordHelper.NormalizeUtc(request.StartedAt);
        var endedAt = PetRecordHelper.NormalizeUtc(request.EndedAt);
        if (startedAt > now.AddDays(1)) throw new ArgumentException("startedAt não pode estar no futuro.");
        if (endedAt.HasValue && endedAt < startedAt) throw new ArgumentException("endedAt não pode ser anterior a startedAt.");
        if (request.Status == WalkStatus.COMPLETED && !endedAt.HasValue) throw new ArgumentException("Passeio concluído exige endedAt.");
        if (request.Status == WalkStatus.IN_PROGRESS && endedAt.HasValue) throw new ArgumentException("Passeio em andamento não pode ter endedAt.");

        var duration = request.DurationSeconds;
        if (!duration.HasValue && endedAt.HasValue) duration = (int)(endedAt.Value - startedAt).TotalSeconds;
        if (duration < 0) throw new ArgumentException("durationSeconds não pode ser negativo.");
        var route = GeoJsonMapper.MapRoute(request.RouteCoordinates);
        var distance = request.DistanceMeters;
        var averageSpeed = request.AverageSpeedKmh;
        if (!averageSpeed.HasValue && distance.HasValue && duration > 0)
        {
            averageSpeed = Math.Round(distance.Value / duration!.Value * 3.6m, 2);
        }

        walk.Status = request.Status;
        walk.StartedAt = startedAt;
        walk.EndedAt = endedAt;
        walk.DurationSeconds = duration;
        walk.DistanceMeters = distance;
        walk.AverageSpeedKmh = averageSpeed;
        walk.Route = route;
        walk.StartLocation = GeoJsonMapper.MapPoint(request.StartLocation, "startLocation");
        walk.EndLocation = GeoJsonMapper.MapPoint(request.EndLocation, "endLocation");
        walk.Weather = request.Weather is null ? null : new WalkWeather { Temperature = request.Weather.Temperature, Condition = PetRecordHelper.NormalizeOptional(request.Weather.Condition) };
        walk.Photos = request.Photos.Select(photo => new WalkPhoto
        {
            Url = photo.Url.Trim(),
            Location = GeoJsonMapper.MapPoint(photo.Location, "photos.location")
        }).ToList();
        walk.Notes = PetRecordHelper.NormalizeOptional(request.Notes);
        walk.UpdatedAt = now;
    }

    internal static void ValidateGeoFilter(double? longitude, double? latitude, double? radiusMeters)
    {
        if (longitude.HasValue != latitude.HasValue) throw new ArgumentException("Informe longitude e latitude juntas.");
        if (radiusMeters.HasValue && !longitude.HasValue) throw new ArgumentException("Informe longitude e latitude junto com radiusMeters.");
        if (radiusMeters.HasValue && radiusMeters <= 0) throw new ArgumentException("radiusMeters deve ser positivo.");
        if (longitude is < -180 or > 180 || latitude is < -90 or > 90) throw new ArgumentException("Coordenadas fora dos limites válidos.");
    }

    internal static void AddRadiusFilter<T>(List<FilterDefinition<T>> filters, string field, double longitude, double latitude, double radiusMeters)
    {
        var radiusRadians = radiusMeters / 6_378_100d;
        filters.Add(new BsonDocumentFilterDefinition<T>(new BsonDocument(field, new BsonDocument("$geoWithin",
            new BsonDocument("$centerSphere", new BsonArray { new BsonArray { longitude, latitude }, radiusRadians })))));
    }

    internal static void AddHasFilter(List<FilterDefinition<Walk>> filters, string field, bool? has)
    {
        if (!has.HasValue) return;
        filters.Add(has.Value
            ? new BsonDocumentFilterDefinition<Walk>(new BsonDocument(field, new BsonDocument("$ne", BsonNull.Value)))
            : new BsonDocumentFilterDefinition<Walk>(new BsonDocument(field, new BsonDocument("$in", new BsonArray { BsonNull.Value }))));
    }

    private static void AddArrayFilter(List<FilterDefinition<Walk>> filters, string field, bool? has)
    {
        if (!has.HasValue) return;
        filters.Add(has.Value
            ? new BsonDocumentFilterDefinition<Walk>(new BsonDocument(field, new BsonDocument("$ne", new BsonArray())))
            : new BsonDocumentFilterDefinition<Walk>(new BsonDocument(field, new BsonArray())));
    }

    private static WalkResponseDto ToResponse(Walk walk) => new(
        walk.Id, walk.PetId, walk.UserId, walk.Status, walk.StartedAt, walk.EndedAt, walk.DurationSeconds,
        walk.DistanceMeters, walk.AverageSpeedKmh, GeoJsonMapper.ToRouteDto(walk.Route),
        GeoJsonMapper.ToPointDto(walk.StartLocation), GeoJsonMapper.ToPointDto(walk.EndLocation),
        walk.Weather is null ? null : new WalkWeatherDto { Temperature = walk.Weather.Temperature, Condition = walk.Weather.Condition },
        walk.Photos.Select(photo => new WalkPhotoDto { Url = photo.Url, Location = GeoJsonMapper.ToPointDto(photo.Location) }).ToList(),
        walk.Notes, walk.CreatedAt, walk.UpdatedAt);
}
