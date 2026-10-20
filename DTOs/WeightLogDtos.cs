using System.ComponentModel.DataAnnotations;

namespace aRevolucaoDosPet.DTOs;

public class WeightLogWriteRequest
{
    [Range(0.01, 1000)]
    public required decimal WeightKg { get; init; }

    [Range(1, 9)]
    public int? BodyScore { get; set; }

    public required DateTime MeasuredAt { get; init; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class CreateWeightLogRequest : WeightLogWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;
}

public class WeightLogSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }

    [Range(0, 1000)]
    public decimal? MinWeightKg { get; set; }

    [Range(0, 1000)]
    public decimal? MaxWeightKg { get; set; }

    [Range(1, 9)]
    public int? MinBodyScore { get; set; }

    [Range(1, 9)]
    public int? MaxBodyScore { get; set; }

    public DateTime? MeasuredAtFrom { get; set; }
    public DateTime? MeasuredAtTo { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
}

public record WeightLogResponseDto(
    string Id,
    string PetId,
    decimal WeightKg,
    int? BodyScore,
    DateTime MeasuredAt,
    string? Notes,
    DateTime CreatedAt
);
