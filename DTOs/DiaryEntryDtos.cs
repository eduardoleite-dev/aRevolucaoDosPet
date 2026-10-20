using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class DiaryMediaDto
{
    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PublicId { get; set; }

    [Required]
    [RegularExpression("^(image|video|audio)$", ErrorMessage = "O tipo da mídia deve ser image, video ou audio.")]
    public string Type { get; set; } = "image";
}

public class DiaryEntryWriteRequest
{
    [StringLength(200)]
    public string? Title { get; set; }

    [StringLength(10000)]
    public string? Content { get; set; }

    [EnumDataType(typeof(DiaryMood))]
    public DiaryMood? Mood { get; set; }

    [EnumDataType(typeof(DiaryLevel))]
    public DiaryLevel? Appetite { get; set; }

    [EnumDataType(typeof(DiaryLevel))]
    public DiaryLevel? Energy { get; set; }

    [EnumDataType(typeof(StoolType))]
    public StoolType? Stool { get; set; }

    [EnumDataType(typeof(UrineType))]
    public UrineType? Urine { get; set; }

    public List<string>? Symptoms { get; set; }

    [MaxLength(10)]
    public List<DiaryMediaDto> Media { get; set; } = [];

    public List<string>? Tags { get; set; }

    // Entradas privadas só aparecem para tutores e administradores.
    public bool IsPrivate { get; set; } = true;

    public string? RelatedExpenseId { get; set; }

    // Padrão: agora. Na edição, omitir mantém a data atual.
    public DateTime? EntryDate { get; set; }
}

public class CreateDiaryEntryRequest : DiaryEntryWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;

    // Só administradores podem registrar em nome de outro usuário.
    public string? AuthorUserId { get; set; }
}

public class DiaryEntrySearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? AuthorUserId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public DiaryMood? Mood { get; set; }
    public DiaryLevel? Appetite { get; set; }
    public DiaryLevel? Energy { get; set; }
    public StoolType? Stool { get; set; }
    public UrineType? Urine { get; set; }
    public string? Symptom { get; set; }
    public string? Tag { get; set; }
    public bool? IsPrivate { get; set; }
    public bool? HasMedia { get; set; }
    public string? RelatedExpenseId { get; set; }
    public bool? HasRelatedExpense { get; set; }
    public DateTime? EntryDateFrom { get; set; }
    public DateTime? EntryDateTo { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record DiaryEntryResponseDto(
    string Id,
    string PetId,
    string AuthorUserId,
    string? Title,
    string? Content,
    DiaryMood? Mood,
    DiaryLevel? Appetite,
    DiaryLevel? Energy,
    StoolType? Stool,
    UrineType? Urine,
    IReadOnlyList<string> Symptoms,
    IReadOnlyList<DiaryMediaDto> Media,
    IReadOnlyList<string> Tags,
    bool IsPrivate,
    string? RelatedExpenseId,
    DateTime EntryDate,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
