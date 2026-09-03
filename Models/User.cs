using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ArquiteturaBaseApi.Models;

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

    [BsonElement("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime? UpdatedAt { get; set; }
    
    [BsonElement("level_access")]
    public int LevelAccess { get; set; }
}