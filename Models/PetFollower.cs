using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class PetFollower
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("follower_user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? FollowerUserId { get; set; }

    [BsonElement("follower_pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? FollowerPetId { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}