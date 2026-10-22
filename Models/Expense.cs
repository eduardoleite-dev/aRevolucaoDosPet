using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Expense
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

    [BsonElement("category")]
    [BsonRepresentation(BsonType.String)]
    public ExpenseCategory Category { get; set; }

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("amount")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal Amount { get; set; }

    [BsonElement("currency")]
    public string Currency { get; set; } = "BRL";

    [BsonElement("expense_date")]
    public DateTime ExpenseDate { get; set; }

    [BsonElement("vendor")]
    public string? Vendor { get; set; }

    [BsonElement("receipt_url")]
    public string? ReceiptUrl { get; set; }

    [BsonElement("is_recurring")]
    public bool IsRecurring { get; set; }

    [BsonElement("recurrence")]
    public ExpenseRecurrence? Recurrence { get; set; }

    [BsonElement("notes")]
    public string? Notes { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class ExpenseRecurrence
{
    [BsonElement("frequency")]
    [BsonRepresentation(BsonType.String)]
    public ExpenseRecurrenceFrequency Frequency { get; set; }

    [BsonElement("interval")]
    public int Interval { get; set; }

    [BsonElement("next_date")]
    public DateTime NextDate { get; set; }
}