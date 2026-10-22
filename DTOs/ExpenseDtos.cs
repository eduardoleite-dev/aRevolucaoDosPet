using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;

namespace aRevolucaoDosPet.DTOs;

public class ExpenseRecurrenceDto
{
    [EnumDataType(typeof(ExpenseRecurrenceFrequency))]
    public required ExpenseRecurrenceFrequency Frequency { get; init; }

    [Range(1, 365)]
    public int Interval { get; set; } = 1;

    public required DateTime NextDate { get; init; }
}

public class ExpenseWriteRequest
{
    [EnumDataType(typeof(ExpenseCategory))]
    public required ExpenseCategory Category { get; init; }

    [Required]
    [StringLength(300)]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100_000_000)]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency deve ser um código de 3 letras, por exemplo BRL.")]
    public string Currency { get; set; } = "BRL";

    public required DateTime ExpenseDate { get; init; }

    [StringLength(200)]
    public string? Vendor { get; set; }

    [Url]
    [StringLength(2048)]
    public string? ReceiptUrl { get; set; }

    public bool IsRecurring { get; set; }
    public ExpenseRecurrenceDto? Recurrence { get; set; }

    [StringLength(5000)]
    public string? Notes { get; set; }

    // Só administradores podem atribuir outro pagador.
    public string? PayerUserId { get; set; }
}

public class CreateExpenseRequest : ExpenseWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;
}

public class ExpenseSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? UserId { get; set; }
    public ExpenseCategory? Category { get; set; }
    public string? Description { get; set; }

    [Range(0, 100_000_000)]
    public decimal? MinAmount { get; set; }

    [Range(0, 100_000_000)]
    public decimal? MaxAmount { get; set; }

    public string? Currency { get; set; }
    public DateTime? ExpenseDateFrom { get; set; }
    public DateTime? ExpenseDateTo { get; set; }
    public string? Vendor { get; set; }
    public bool? HasReceipt { get; set; }
    public bool? IsRecurring { get; set; }
    public ExpenseRecurrenceFrequency? RecurrenceFrequency { get; set; }
    public int? RecurrenceInterval { get; set; }
    public DateTime? NextDateFrom { get; set; }
    public DateTime? NextDateTo { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
}

public record ExpenseResponseDto(
    string Id,
    string PetId,
    string UserId,
    ExpenseCategory Category,
    string Description,
    decimal Amount,
    string Currency,
    DateTime ExpenseDate,
    string? Vendor,
    string? ReceiptUrl,
    bool IsRecurring,
    ExpenseRecurrenceDto? Recurrence,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
