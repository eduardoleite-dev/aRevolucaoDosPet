using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("avatar_url")]
    public string? AvatarUrl { get; set; }

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("city")]
    public string? City { get; set; }

    [BsonElement("state")]
    public string? State { get; set; }

    [BsonElement("country")]
    public string Country { get; set; } = "BR";

    [BsonElement("location")]
    public UserLocation? Location { get; set; }

    [BsonElement("access_level")]
    public AccessLevel AccessLevel { get; set; } = AccessLevel.TUTOR;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;

    [BsonElement("email_verified_at")]
    public DateTime? EmailVerifiedAt { get; set; }

    [BsonElement("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class UserLocation
{
    [BsonElement("type")]
    public string Type { get; set; } = "Point";

    [BsonElement("coordinates")]
    public double[] Coordinates { get; set; } = [];
}