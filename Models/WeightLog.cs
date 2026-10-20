using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class WeightLog
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("weight_kg")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal WeightKg { get; set; }

    [BsonElement("body_score")]
    public int? BodyScore { get; set; }

    [BsonElement("measured_at")]
    public DateTime MeasuredAt { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
