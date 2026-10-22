using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class CreateNotificationRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    public string? PetId { get; set; }

    [EnumDataType(typeof(NotificationType))]
    public required NotificationType Type { get; init; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Body { get; set; } = string.Empty;

    public Dictionary<string, JsonElement>? Data { get; set; }
}

public class NotificationSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? UserId { get; set; }
    public string? PetId { get; set; }
    public NotificationType? Type { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? ReadAtFrom { get; set; }
    public DateTime? ReadAtTo { get; set; }
}

public record NotificationResponseDto(
    string Id,
    string UserId,
    string? PetId,
    NotificationType Type,
    string Title,
    string Body,
    Dictionary<string, JsonElement>? Data,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt
);
