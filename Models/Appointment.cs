using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Appointment
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public AppointmentType Type { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    [BsonElement("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.SCHEDULED;

    [BsonElement("veterinarian_name")]
    public string? VeterinarianName { get; set; }

    [BsonElement("veterinarian_crmv")]
    public string? VeterinarianCrmv { get; set; }

    [BsonElement("clinic_name")]
    public string? ClinicName { get; set; }

    [BsonElement("clinic_address")]
    public string? ClinicAddress { get; set; }

    [BsonElement("price")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? Price { get; set; }

    [BsonElement("diagnosis")]
    public string? Diagnosis { get; set; }

    [BsonElement("prescription_url")]
    public string? PrescriptionUrl { get; set; }

    [BsonElement("attachments")]
    public List<AppointmentAttachment> Attachments { get; set; } = [];

    [BsonElement("reminder_sent")]
    public bool ReminderSent { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class AppointmentAttachment
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("public_id")]
    public string? PublicId { get; set; }

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("name")]
    public string? Name { get; set; }
}
