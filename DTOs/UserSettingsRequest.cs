using System.ComponentModel.DataAnnotations;

namespace aRevolucaoDosPet.DTOs;

public class UserSettingsRequest
{
    [Required]
    public NotificationSettingsDto? Notifications { get; set; }

    [Required]
    public PrivacySettingsDto? Privacy { get; set; }

    [Required]
    [StringLength(30)]
    public string Theme { get; set; } = "light";

    [Required]
    [StringLength(20)]
    public string Language { get; set; } = "pt-BR";
}

public class NotificationSettingsDto
{
    public required bool VaccineDue { get; init; }
    public required bool Appointment { get; init; }
    public required bool HeatCycle { get; init; }
    public required bool Likes { get; init; }
    public required bool Comments { get; init; }
    public required bool Followers { get; init; }
    public required bool Nearby { get; init; }
    public required bool Expenses { get; init; }
}

public class PrivacySettingsDto
{
    public required bool ShowLocation { get; init; }
    public required bool AllowDm { get; init; }
    public required bool PublicProfile { get; init; }
}