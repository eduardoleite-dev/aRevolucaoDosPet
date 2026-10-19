using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class UserSettings
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("notifications")]
    public NotificationSettings Notifications { get; set; } = new();

    [BsonElement("privacy")]
    public PrivacySettings Privacy { get; set; } = new();

    [BsonElement("theme")]
    public string Theme { get; set; } = "light";

    [BsonElement("language")]
    public string Language { get; set; } = "pt-BR";

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class NotificationSettings
{
    [BsonElement("vaccine_due")]
    public bool VaccineDue { get; set; }

    [BsonElement("appointment")]
    public bool Appointment { get; set; }

    [BsonElement("heat_cycle")]
    public bool HeatCycle { get; set; }

    [BsonElement("likes")]
    public bool Likes { get; set; }

    [BsonElement("comments")]
    public bool Comments { get; set; }

    [BsonElement("followers")]
    public bool Followers { get; set; }

    [BsonElement("nearby")]
    public bool Nearby { get; set; }

    [BsonElement("expenses")]
    public bool Expenses { get; set; }
}

public class PrivacySettings
{
    [BsonElement("show_location")]
    public bool ShowLocation { get; set; }

    [BsonElement("allow_dm")]
    public bool AllowDm { get; set; }

    [BsonElement("public_profile")]
    public bool PublicProfile { get; set; }
}