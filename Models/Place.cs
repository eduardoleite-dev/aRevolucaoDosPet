using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Place
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public PlaceType Type { get; set; }

    [BsonElement("address")]
    public string? Address { get; set; }

    [BsonElement("location")]
    public UserLocation Location { get; set; } = new();

    [BsonElement("is_pet_friendly")]
    public bool IsPetFriendly { get; set; } = true;

    [BsonElement("has_water")]
    public bool HasWater { get; set; }

    [BsonElement("has_shade")]
    public bool HasShade { get; set; }

    [BsonElement("is_fenced")]
    public bool IsFenced { get; set; }

    [BsonElement("average_rating")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal AverageRating { get; set; }

    [BsonElement("reviews_count")]
    public int ReviewsCount { get; set; }

    [BsonElement("hazards")]
    public List<string> Hazards { get; set; } = [];

    [BsonElement("created_by_user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? CreatedByUserId { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlaceReview
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("place_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PlaceId { get; set; } = string.Empty;

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PetId { get; set; }

    [BsonElement("rating")]
    public int Rating { get; set; }

    [BsonElement("comment")]
    public string? Comment { get; set; }

    [BsonElement("photos")]
    public List<PlaceReviewPhoto> Photos { get; set; } = [];

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class PlaceReviewPhoto
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("public_id")]
    public string? PublicId { get; set; }
}
