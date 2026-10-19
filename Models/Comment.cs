using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Comment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("post_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PostId { get; set; }

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? PetId { get; set; }

    [BsonElement("author_user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorUserId { get; set; } = string.Empty;

    [BsonElement("parent_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ParentId { get; set; }

    [BsonElement("content")]
    public string Content { get; set; } = string.Empty;

    [BsonElement("attachments")]
    public List<CommentAttachment> Attachments { get; set; } = [];

    [BsonElement("reactions")]
    public List<CommentReaction> Reactions { get; set; } = [];

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class CommentAttachment
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("public_id")]
    public string? PublicId { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "image";
}

public class CommentReaction
{
    [BsonElement("user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("emoji")]
    public string Emoji { get; set; } = string.Empty;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
