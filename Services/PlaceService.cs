using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PlaceService : IPlaceService
{
    private readonly IMongoCollection<Place> _places;
    private readonly IMongoCollection<PlaceReview> _reviews;

    public PlaceService(IMongoDatabase database)
    {
        _places = database.GetCollection<Place>("Places");
        _reviews = database.GetCollection<PlaceReview>("PlaceReviews");
    }

    public async Task<PlaceResponseDto> CreateAsync(PlaceWriteRequest request, string? currentUserId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var place = new Place
        {
            CreatedByUserId = ObjectId.TryParse(currentUserId, out _) ? currentUserId : null,
            CreatedAt = now,
            UpdatedAt = now
        };
        Apply(place, request, now);
        await _places.InsertOneAsync(place, cancellationToken: cancellationToken);
        return ToResponse(place);
    }

    public async Task<PagedResult<PlaceResponseDto>> SearchAsync(PlaceSearchFilter filter, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<Place>>();
        var builder = Builders<Place>.Filter;
        PetRecordHelper.AddObjectId(filters, "_id", filter.Id);
        PetRecordHelper.AddText(filters, "name", filter.Name);
        PetRecordHelper.AddText(filters, "address", filter.Address);
        PetRecordHelper.AddText(filters, "hazards", filter.Hazard);
        if (filter.Type.HasValue) filters.Add(builder.Eq(place => place.Type, filter.Type.Value));
        if (filter.IsPetFriendly.HasValue) filters.Add(builder.Eq(place => place.IsPetFriendly, filter.IsPetFriendly.Value));
        if (filter.HasWater.HasValue) filters.Add(builder.Eq(place => place.HasWater, filter.HasWater.Value));
        if (filter.HasShade.HasValue) filters.Add(builder.Eq(place => place.HasShade, filter.HasShade.Value));
        if (filter.IsFenced.HasValue) filters.Add(builder.Eq(place => place.IsFenced, filter.IsFenced.Value));
        if (filter.MinRating.HasValue) filters.Add(builder.Gte(place => place.AverageRating, (decimal)filter.MinRating.Value));
        if (filter.MaxRating.HasValue) filters.Add(builder.Lte(place => place.AverageRating, (decimal)filter.MaxRating.Value));

        WalkService.ValidateGeoFilter(filter.Longitude, filter.Latitude, filter.RadiusMeters);
        if (filter.RadiusMeters.HasValue)
        {
            WalkService.AddRadiusFilter(filters, "location", filter.Longitude!.Value, filter.Latitude!.Value, filter.RadiusMeters.Value);
        }

        var query = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var count = await _places.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var places = await _places.Find(query).SortByDescending(place => place.AverageRating).ThenBy(place => place.Id)
            .Skip(skip).Limit(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<PlaceResponseDto>(places.Select(ToResponse).ToList(), count, filter.PageNumber, filter.PageSize);
    }

    public async Task<PlaceResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var place = await _places.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return place is null ? null : ToResponse(place);
    }

    public async Task<PlaceResponseDto?> UpdateAsync(string id, PlaceWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var place = await FindEditableAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (place is null) return null;
        Apply(place, request, DateTime.UtcNow);
        await _places.ReplaceOneAsync(item => item.Id == id, place, cancellationToken: cancellationToken);
        return ToResponse(place);
    }

    public async Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var place = await FindEditableAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (place is null) return false;
        await _reviews.DeleteManyAsync(review => review.PlaceId == id, cancellationToken);
        return (await _places.DeleteOneAsync(item => item.Id == id, cancellationToken)).DeletedCount == 1;
    }

    private async Task<Place?> FindEditableAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var place = await _places.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (place is null) return null;
        if (!isAdministrator && place.CreatedByUserId != currentUserId) throw new RecordPermissionException();
        return place;
    }

    private static void Apply(Place place, PlaceWriteRequest request, DateTime now)
    {
        place.Name = request.Name.Trim();
        place.Type = request.Type;
        place.Address = PetRecordHelper.NormalizeOptional(request.Address);
        var location = GeoJsonMapper.MapPoint(request.Location, "location", required: true);
        place.Location = location;
        place.IsPetFriendly = request.IsPetFriendly;
        place.HasWater = request.HasWater;
        place.HasShade = request.HasShade;
        place.IsFenced = request.IsFenced;
        place.Hazards = request.Hazards.Where(hazard => !string.IsNullOrWhiteSpace(hazard))
            .Select(hazard => hazard.Trim().ToLowerInvariant()).Distinct().Take(30).ToList();
        place.UpdatedAt = now;
    }

    private static PlaceResponseDto ToResponse(Place place) => new(
        place.Id, place.Name, place.Type, place.Address, GeoJsonMapper.ToPointDto(place.Location)!,
        place.IsPetFriendly, place.HasWater, place.HasShade, place.IsFenced, place.AverageRating,
        place.ReviewsCount, place.Hazards, place.CreatedByUserId, place.CreatedAt, place.UpdatedAt);
}
