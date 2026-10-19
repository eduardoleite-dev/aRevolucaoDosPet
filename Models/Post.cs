using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Post
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("author_user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorUserId { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public PostType Type { get; set; }

    [BsonElement("caption")]
    public string? Caption { get; set; }

    [BsonElement("media")]
    public List<PostMedia> Media { get; set; } = [];

    [BsonElement("location")]
    public PostLocation? Location { get; set; }

    [BsonElement("tagged_pet_ids")]
    public List<ObjectId> TaggedPetIds { get; set; } = [];

    [BsonElement("tagged_user_ids")]
    public List<ObjectId> TaggedUserIds { get; set; } = [];

    [BsonElement("hashtags")]
    public List<string> Hashtags { get; set; } = [];

    [BsonElement("visibility")]
    [BsonRepresentation(BsonType.String)]
    public PostVisibility Visibility { get; set; } = PostVisibility.PUBLIC;

    [BsonElement("likes_count")]
    public int LikesCount { get; set; }

    [BsonElement("comments_count")]
    public int CommentsCount { get; set; }

    [BsonElement("shares_count")]
    public int SharesCount { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class PostMedia
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("public_id")]
    public string? PublicId { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "image";

    [BsonElement("width")]
    public int? Width { get; set; }

    [BsonElement("height")]
    public int? Height { get; set; }

    [BsonElement("duration_seconds")]
    public double? DurationSeconds { get; set; }
}

public class PostLocation : UserLocation
{
    [BsonElement("name")]
    public string? Name { get; set; }

    [BsonElement("place_id")]
    public string? PlaceId { get; set; }
}
