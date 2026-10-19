using System.ComponentModel.DataAnnotations;

namespace aRevolucaoDosPet.DTOs;

public class CommentAttachmentDto
{
    [Required]
    [Url]
    [StringLength(2048)]
    public string Url { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PublicId { get; set; }

    [Required]
    [RegularExpression("^(image|video|audio)$", ErrorMessage = "O tipo do anexo deve ser image, video ou audio.")]
    public string Type { get; set; } = "image";
}

public class CommentWriteRequest
{
    [Required]
    [StringLength(2000)]
    public string Content { get; set; } = string.Empty;

    [MaxLength(4)]
    public List<CommentAttachmentDto> Attachments { get; set; } = [];
}

public class CreateCommentRequest : CommentWriteRequest
{
    [Required]
    public string PostId { get; set; } = string.Empty;

    // Informe para comentar em nome de um pet.
    public string? PetId { get; set; }

    // Informe para responder a um comentário de primeiro nível.
    public string? ParentId { get; set; }
}

public class ReactToCommentRequest
{
    [Required]
    [StringLength(16, MinimumLength = 1)]
    public string Emoji { get; set; } = string.Empty;
}

public class CommentSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PostId { get; set; }
    public string? PetId { get; set; }
    public string? AuthorUserId { get; set; }
    public string? ParentId { get; set; }
    public bool? RootOnly { get; set; }
    public string? Content { get; set; }
    public bool? HasAttachments { get; set; }
    public string? Emoji { get; set; }
    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
    public DateTime? DeletedAtFrom { get; set; }
    public DateTime? DeletedAtTo { get; set; }
    public bool IncludeDeleted { get; set; }
}

public record CommentReactionDto(string UserId, string Emoji, DateTime CreatedAt);

public record CommentResponseDto(
    string Id,
    string? PostId,
    string? PetId,
    string AuthorUserId,
    string? ParentId,
    string Content,
    IReadOnlyList<CommentAttachmentDto> Attachments,
    IReadOnlyList<CommentReactionDto> Reactions,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);
