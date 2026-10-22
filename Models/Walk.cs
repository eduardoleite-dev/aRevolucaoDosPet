using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Walk
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public WalkStatus Status { get; set; } = WalkStatus.IN_PROGRESS;

    [BsonElement("started_at")]
    public DateTime StartedAt { get; set; }

    [BsonElement("ended_at")]
    public DateTime? EndedAt { get; set; }

    [BsonElement("duration_seconds")]
    public int? DurationSeconds { get; set; }

    [BsonElement("distance_meters")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? DistanceMeters { get; set; }

    [BsonElement("average_speed_kmh")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? AverageSpeedKmh { get; set; }

    [BsonElement("route")]
    public WalkRoute? Route { get; set; }

    [BsonElement("start_location")]
    public UserLocation? StartLocation { get; set; }

    [BsonElement("end_location")]
    public UserLocation? EndLocation { get; set; }

    [BsonElement("weather")]
    public WalkWeather? Weather { get; set; }

    [BsonElement("photos")]
    public List<WalkPhoto> Photos { get; set; } = [];

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class WalkRoute
{
    [BsonElement("type")]
    public string Type { get; set; } = "LineString";

    [BsonElement("coordinates")]
    public List<double[]> Coordinates { get; set; } = [];
}

public class WalkWeather
{
    [BsonElement("temperature")]
    public double? Temperature { get; set; }

    [BsonElement("condition")]
    public string? Condition { get; set; }
}

public class WalkPhoto
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("location")]
    public UserLocation? Location { get; set; }
}
