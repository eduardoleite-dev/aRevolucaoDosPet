using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class WalkEncounter
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("walk_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string WalkId { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("other_pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? OtherPetId { get; set; }

    [BsonElement("other_pet_name")]
    public string? OtherPetName { get; set; }

    [BsonElement("location")]
    public UserLocation? Location { get; set; }

    [BsonElement("interaction_type")]
    [BsonRepresentation(BsonType.String)]
    public WalkInteractionType? InteractionType { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
