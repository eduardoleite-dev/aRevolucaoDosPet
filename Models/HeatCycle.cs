using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class HeatCycle
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("started_at")]
    public DateTime StartedAt { get; set; }

    [BsonElement("ended_at")]
    public DateTime? EndedAt { get; set; }

    [BsonElement("phase")]
    [BsonRepresentation(BsonType.String)]
    public HeatCyclePhase? Phase { get; set; }

    [BsonElement("symptoms")]
    public List<string> Symptoms { get; set; } = [];

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("predicted_next_at")]
    public DateTime? PredictedNextAt { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}