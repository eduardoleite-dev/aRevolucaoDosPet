using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class PostReaction
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("post_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PostId { get; set; } = string.Empty;

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PetId { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public ReactionType Type { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
