using aRevolucaoDosPet.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace aRevolucaoDosPet.Models;

public class Pet
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("owner_id")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string OwnerId { get; set; } = string.Empty;

    [BsonElement("co_tutor_ids")]
    public List<ObjectId> CoTutorIds { get; set; } = [];

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("username")]
    public string? Username { get; set; }

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("avatar_url")]
    public string? AvatarUrl { get; set; }

    [BsonElement("cover_url")]
    public string? CoverUrl { get; set; }

    [BsonElement("species")]
    [BsonRepresentation(BsonType.String)]
    public Species Species { get; set; }

    [BsonElement("breed")]
    public string? Breed { get; set; }

    [BsonElement("sex")]
    [BsonRepresentation(BsonType.String)]
    public Sex Sex { get; set; } = Sex.UNKNOWN;

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("birth_date")]
    public DateTime? BirthDate { get; set; }

    [BsonElement("adoption_date")]
    public DateTime? AdoptionDate { get; set; }

    [BsonElement("weight_kg")]
    [BsonRepresentation(BsonType.Decimal128)]
    public decimal? WeightKg { get; set; }

    [BsonElement("size")]
    [BsonRepresentation(BsonType.String)]
    public PetSize? Size { get; set; }

    [BsonElement("microchip")]
    public string? Microchip { get; set; }

    [BsonElement("pedigree")]
    public string? Pedigree { get; set; }

    [BsonElement("is_neutered")]
    public bool IsNeutered { get; set; }

    [BsonElement("is_breeder")]
    public bool IsBreeder { get; set; }

    [BsonElement("allergies")]
    public List<string> Allergies { get; set; } = [];

    [BsonElement("chronic_conditions")]
    public List<string> ChronicConditions { get; set; } = [];

    [BsonElement("continuous_medications")]
    public List<PetMedication> ContinuousMedications { get; set; } = [];

    [BsonElement("vet_name")]
    public string? VetName { get; set; }

    [BsonElement("vet_phone")]
    public string? VetPhone { get; set; }

    [BsonElement("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }

    [BsonElement("emergency_contact_phone")]
    public string? EmergencyContactPhone { get; set; }

    [BsonElement("health_plan")]
    public string? HealthPlan { get; set; }

    [BsonElement("health_plan_number")]
    public string? HealthPlanNumber { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public PetStatus Status { get; set; } = PetStatus.ALIVE;

    [BsonElement("visibility")]
    [BsonRepresentation(BsonType.String)]
    public PetProfileVisibility Visibility { get; set; } = PetProfileVisibility.PUBLIC;

    [BsonElement("location")]
    public UserLocation? Location { get; set; }

    [BsonElement("followers_count")]
    public int FollowersCount { get; set; }

    [BsonElement("following_count")]
    public int FollowingCount { get; set; }

    [BsonElement("posts_count")]
    public int PostsCount { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("deleted_at")]
    public DateTime? DeletedAt { get; set; }
}

public class PetMedication
{
    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("dosage")]
    public string Dosage { get; set; } = string.Empty;

    [BsonElement("frequency")]
    public string Frequency { get; set; } = string.Empty;

    [BsonElement("started_at")]
    public DateTime StartedAt { get; set; }
}