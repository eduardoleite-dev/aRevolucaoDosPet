using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Vaccine
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("pet_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string PetId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("laboratory")]
    public string? Laboratory { get; set; }

    [BsonElement("batch_number")]
    public string? BatchNumber { get; set; }

    [BsonElement("dose_number")]
    public int? DoseNumber { get; set; }

    [BsonElement("applied_at")]
    public DateTime? AppliedAt { get; set; }

    [BsonElement("next_dose_at")]
    public DateTime? NextDoseAt { get; set; }

    [BsonElement("veterinarian_name")]
    public string? VeterinarianName { get; set; }

    [BsonElement("veterinarian_crmv")]
    public string? VeterinarianCrmv { get; set; }

    [BsonElement("clinic_name")]
    public string? ClinicName { get; set; }

    [BsonElement("certificate_url")]
    public string? CertificateUrl { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public VaccineStatus Status { get; set; } = VaccineStatus.APPLIED;

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
