using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class UserSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? LocationType { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(0.01, 20_000_000)]
    public double? RadiusMeters { get; set; }

    public AccessLevel? AccessLevel { get; set; }
    public UserStatus? Status { get; set; }
    public bool? EmailVerified { get; set; }
    public bool? HasLoggedIn { get; set; }
    public bool? HasLocation { get; set; }
    public bool? IsDeleted { get; set; }
    public bool IncludeDeleted { get; set; }

    public DateTime? EmailVerifiedAtFrom { get; set; }
    public DateTime? EmailVerifiedAtTo { get; set; }
    public DateTime? LastLoginAtFrom { get; set; }
    public DateTime? LastLoginAtTo { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
    public DateTime? DeletedAtFrom { get; set; }
    public DateTime? DeletedAtTo { get; set; }
}