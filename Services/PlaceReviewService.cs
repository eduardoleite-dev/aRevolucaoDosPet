using aRevolucaoDosPet.DTOs;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;
using aRevolucaoDosPet.Services.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace aRevolucaoDosPet.Services;

public class PlaceReviewService : IPlaceReviewService
{
    private readonly IMongoCollection<PlaceReview> _reviews;
    private readonly IMongoCollection<Place> _places;
    private readonly IMongoCollection<Pet> _pets;
    private readonly IMongoCollection<User> _users;

    public PlaceReviewService(IMongoDatabase database)
    {
        _reviews = database.GetCollection<PlaceReview>("PlaceReviews");
        _places = database.GetCollection<Place>("Places");
        _pets = database.GetCollection<Pet>("Pets");
        _users = database.GetCollection<User>("Users");
    }

    public async Task<PlaceReviewResponseDto> UpsertAsync(string placeId, PlaceReviewWriteRequest request, string? currentUserId, CancellationToken cancellationToken = default)
    {
        PetRecordHelper.EnsureObjectId(placeId, "placeId");
        PetRecordHelper.EnsureObjectId(currentUserId, "userId");
        if (!await _places.Find(place => place.Id == placeId).AnyAsync(cancellationToken)) throw new PlacePetNotFoundException();
        if (!await _users.Find(user => user.Id == currentUserId && user.Status == UserStatus.ACTIVE).AnyAsync(cancellationToken))
            throw new PlaceReviewUserNotFoundException();

        var petId = PetRecordHelper.NormalizeOptional(request.PetId);
        if (petId is not null)
        {
            PetRecordHelper.EnsureObjectId(petId, "petId");
            var pet = await _pets.Find(item => item.Id == petId && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
            if (pet is null) throw new WalkPetNotFoundException();
            if (!PetRecordHelper.CanEdit(pet, currentUserId)) throw new PlaceReviewPermissionException();
        }

        var now = DateTime.UtcNow;
        var existing = await _reviews.Find(review => review.PlaceId == placeId && review.UserId == currentUserId)
            .FirstOrDefaultAsync(cancellationToken);
        var review = new PlaceReview
        {
            PlaceId = placeId,
            UserId = currentUserId!,
            PetId = petId,
            Rating = request.Rating,
            Comment = PetRecordHelper.NormalizeOptional(request.Comment),
            Photos = MapPhotos(request.Photos),
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        };

        if (existing is null)
        {
            try
            {
                await _reviews.InsertOneAsync(review, cancellationToken: cancellationToken);
            }
            catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                existing = await _reviews.Find(item => item.PlaceId == placeId && item.UserId == currentUserId).FirstAsync(cancellationToken);
                review.Id = existing.Id;
                review.CreatedAt = existing.CreatedAt;
                await _reviews.ReplaceOneAsync(item => item.Id == existing.Id, review, cancellationToken: cancellationToken);
            }
        }
        else
        {
            review.Id = existing.Id;
            await _reviews.ReplaceOneAsync(item => item.Id == existing.Id, review, cancellationToken: cancellationToken);
        }

        await RecalculateRatingAsync(placeId, cancellationToken);
        return ToResponse(review);
    }

    public async Task<PagedResult<PlaceReviewResponseDto>?> SearchAsync(string placeId, PlaceReviewSearchFilter filter, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(placeId, out _)) return null;
        if (!await _places.Find(place => place.Id == placeId).AnyAsync(cancellationToken)) return null;

        var filters = new List<FilterDefinition<PlaceReview>> { Builders<PlaceReview>.Filter.Eq(review => review.PlaceId, placeId) };
        var builder = Builders<PlaceReview>.Filter;
        if (filter.Rating.HasValue)
        {
            if (filter.Rating is < 1 or > 5) throw new ArgumentException("rating deve estar entre 1 e 5.");
            filters.Add(builder.Eq(review => review.Rating, filter.Rating.Value));
        }
        PetRecordHelper.AddObjectId(filters, "user_id", filter.UserId);
        PetRecordHelper.AddObjectId(filters, "pet_id", filter.PetId);
        if (filter.HasComment.HasValue) filters.Add(filter.HasComment.Value ? builder.Ne(review => review.Comment, null) : builder.Eq(review => review.Comment, null));
        if (filter.HasPhotos.HasValue) filters.Add(filter.HasPhotos.Value ? builder.SizeGt(review => review.Photos, 0) : builder.Size(review => review.Photos, 0));
        PetRecordHelper.AddDateRange(filters, "created_at", filter.CreatedAtFrom, filter.CreatedAtTo);

        var query = builder.And(filters);
        var skip = (filter.PageNumber - 1) * filter.PageSize;
        var count = await _reviews.CountDocumentsAsync(query, cancellationToken: cancellationToken);
        var reviews = await _reviews.Find(query).SortByDescending(review => review.CreatedAt).ThenBy(review => review.Id)
            .Skip(skip).Limit(filter.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<PlaceReviewResponseDto>(reviews.Select(ToResponse).ToList(), count, filter.PageNumber, filter.PageSize);
    }

    public async Task<PlaceReviewResponseDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var review = await _reviews.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        return review is null ? null : ToResponse(review);
    }

    public async Task<PlaceReviewResponseDto?> UpdateAsync(string id, PlaceReviewWriteRequest request, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var review = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (review is null) return null;
        await ValidateReviewPetAsync(request.PetId, currentUserId, cancellationToken);
        review.Rating = request.Rating;
        review.Comment = PetRecordHelper.NormalizeOptional(request.Comment);
        review.PetId = PetRecordHelper.NormalizeOptional(request.PetId);
        review.Photos = MapPhotos(request.Photos);
        review.UpdatedAt = DateTime.UtcNow;
        await _reviews.ReplaceOneAsync(item => item.Id == id, review, cancellationToken: cancellationToken);
        await RecalculateRatingAsync(review.PlaceId, cancellationToken);
        return ToResponse(review);
    }

    public async Task<bool> DeleteAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken = default)
    {
        var review = await FindAuthorizedAsync(id, currentUserId, isAdministrator, cancellationToken);
        if (review is null) return false;
        await _reviews.DeleteOneAsync(item => item.Id == id, cancellationToken);
        await RecalculateRatingAsync(review.PlaceId, cancellationToken);
        return true;
    }

    private async Task<PlaceReview?> FindAuthorizedAsync(string id, string? currentUserId, bool isAdministrator, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(id, out _)) return null;
        var review = await _reviews.Find(item => item.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (review is null) return null;
        if (!isAdministrator && review.UserId != currentUserId) throw new PlaceReviewPermissionException();
        return review;
    }

    private async Task ValidateReviewPetAsync(string? petId, string? userId, CancellationToken cancellationToken)
    {
        var normalized = PetRecordHelper.NormalizeOptional(petId);
        if (normalized is null) return;
        PetRecordHelper.EnsureObjectId(normalized, "petId");
        var pet = await _pets.Find(item => item.Id == normalized && item.DeletedAt == null).FirstOrDefaultAsync(cancellationToken);
        if (pet is null) throw new WalkPetNotFoundException();
        if (!PetRecordHelper.CanEdit(pet, userId)) throw new PlaceReviewPermissionException();
    }

    private async Task RecalculateRatingAsync(string placeId, CancellationToken cancellationToken)
    {
        var ratings = await _reviews.Find(review => review.PlaceId == placeId).Project(review => review.Rating).ToListAsync(cancellationToken);
        var average = ratings.Count == 0 ? 0m : Math.Round((decimal)ratings.Average(), 2, MidpointRounding.AwayFromZero);
        await _places.UpdateOneAsync(place => place.Id == placeId,
            Builders<Place>.Update.Set(place => place.AverageRating, average).Set(place => place.ReviewsCount, ratings.Count).Set(place => place.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    private static List<PlaceReviewPhoto> MapPhotos(IEnumerable<PlaceReviewPhotoDto>? photos) => photos?.Select(photo => new PlaceReviewPhoto
    {
        Url = photo.Url.Trim(),
        PublicId = PetRecordHelper.NormalizeOptional(photo.PublicId)
    }).ToList() ?? [];

    private static PlaceReviewResponseDto ToResponse(PlaceReview review) => new(
        review.Id, review.PlaceId, review.UserId, review.PetId, review.Rating, review.Comment,
        review.Photos.Select(photo => new PlaceReviewPhotoDto { Url = photo.Url, PublicId = photo.PublicId }).ToList(),
        review.CreatedAt, review.UpdatedAt);
}
