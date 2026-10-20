using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class DiaryEntry
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("author_user_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string AuthorUserId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string? Title { get; set; }

    [BsonElement("content")]
    public string? Content { get; set; }

    [BsonElement("mood")]
    [BsonRepresentation(BsonType.String)]
    public DiaryMood? Mood { get; set; }

    [BsonElement("appetite")]
    [BsonRepresentation(BsonType.String)]
    public DiaryLevel? Appetite { get; set; }

    [BsonElement("energy")]
    [BsonRepresentation(BsonType.String)]
    public DiaryLevel? Energy { get; set; }

    [BsonElement("stool")]
    [BsonRepresentation(BsonType.String)]
    public StoolType? Stool { get; set; }

    [BsonElement("urine")]
    [BsonRepresentation(BsonType.String)]
    public UrineType? Urine { get; set; }

    [BsonElement("symptoms")]
    public List<string> Symptoms { get; set; } = [];

    [BsonElement("media")]
    public List<DiaryMedia> Media { get; set; } = [];

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = [];

    [BsonElement("is_private")]
    public bool IsPrivate { get; set; } = true;

    [BsonElement("related_expense_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? RelatedExpenseId { get; set; }

    [BsonElement("entry_date")]
    public DateTime EntryDate { get; set; } = DateTime.UtcNow;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class DiaryMedia
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("public_id")]
    public string? PublicId { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = "image";
}
