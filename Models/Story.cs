using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Story
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

    [BsonElement("media_url")]
    public string MediaUrl { get; set; } = string.Empty;

    [BsonElement("media_type")]
    public string MediaType { get; set; } = "image";

    [BsonElement("caption")]
    public string? Caption { get; set; }

    [BsonElement("views_count")]
    public int ViewsCount { get; set; }

    [BsonElement("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
