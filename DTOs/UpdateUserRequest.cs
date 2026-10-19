using System.ComponentModel.DataAnnotations;

namespace aRevolucaoDosPet.DTOs;

public class UpdateUserRequest
{
    [Required]
    [StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Url]
    [StringLength(2048)]
    public string? AvatarUrl { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
    public string? State { get; set; }

    [StringLength(2, MinimumLength = 2)]
    public string Country { get; set; } = "BR";

    public UserLocationRequest? Location { get; set; }
}