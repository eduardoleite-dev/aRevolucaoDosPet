using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Pregnancy
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("father_pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? FatherPetId { get; set; }

    [BsonElement("father_name")]
    public string? FatherName { get; set; }

    [BsonElement("started_at")]
    public DateTime StartedAt { get; set; }

    [BsonElement("expected_birth_at")]
    public DateTime? ExpectedBirthAt { get; set; }

    [BsonElement("actual_birth_at")]
    public DateTime? ActualBirthAt { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public PregnancyStatus Status { get; set; } = PregnancyStatus.ONGOING;

    [BsonElement("ultrasound_url")]
    public string? UltrasoundUrl { get; set; }

    [BsonElement("puppies_count")]
    public int? PuppiesCount { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}