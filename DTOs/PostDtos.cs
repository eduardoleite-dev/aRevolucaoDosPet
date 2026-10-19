using System.ComponentModel.DataAnnotations;
using aRevolucaoDosPet.Enums;
using aRevolucaoDosPet.Models;

namespace aRevolucaoDosPet.DTOs;

public class PostMediaDto
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

    [Range(1, 20000)]
    public int? Width { get; set; }

    [Range(1, 20000)]
    public int? Height { get; set; }

    [Range(0.01, 86400)]
    public double? DurationSeconds { get; set; }
}

public class PostLocationRequest : UserLocationRequest
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(200)]
    public string? PlaceId { get; set; }
}

public class PostWriteRequest
{
    [StringLength(5000)]
    public string? Caption { get; set; }

    public PostLocationRequest? Location { get; set; }
    public List<string>? TaggedPetIds { get; set; }
    public List<string>? TaggedUserIds { get; set; }
    public List<string>? Hashtags { get; set; }

    [EnumDataType(typeof(PostVisibility))]
    public PostVisibility Visibility { get; set; } = PostVisibility.PUBLIC;
}

public class CreatePostRequest : PostWriteRequest
{
    [Required]
    public string PetId { get; set; } = string.Empty;

    // Só administradores podem publicar em nome de outro usuário.
    public string? AuthorUserId { get; set; }

    [EnumDataType(typeof(PostType))]
    public required PostType Type { get; init; }

    [Required]
    public List<PostMediaDto> Media { get; set; } = [];
}

public class PostSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public string? Id { get; set; }
    public string? PetId { get; set; }
    public string? AuthorUserId { get; set; }
    public PostType? Type { get; set; }
    public string? Caption { get; set; }
    public string? MediaType { get; set; }
    public string? Hashtag { get; set; }
    public string? TaggedPetId { get; set; }
    public string? TaggedUserId { get; set; }
    public PostVisibility? Visibility { get; set; }
    public string? LocationName { get; set; }
    public string? PlaceId { get; set; }
    public bool? HasLocation { get; set; }

    [Range(-180, 180)]
    public double? Longitude { get; set; }

    [Range(-90, 90)]
    public double? Latitude { get; set; }

    [Range(0.01, 20_000_000)]
    public double? RadiusMeters { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinLikesCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxLikesCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinCommentsCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxCommentsCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MinSharesCount { get; set; }

    [Range(0, int.MaxValue)]
    public int? MaxSharesCount { get; set; }

    public DateTime? CreatedAtFrom { get; set; }
    public DateTime? CreatedAtTo { get; set; }
    public DateTime? UpdatedAtFrom { get; set; }
    public DateTime? UpdatedAtTo { get; set; }
    public DateTime? DeletedAtFrom { get; set; }
    public DateTime? DeletedAtTo { get; set; }
    public bool IncludeDeleted { get; set; }
}

public record PostResponseDto(
    string Id,
    string PetId,
    string AuthorUserId,
    PostType Type,
    string? Caption,
    IReadOnlyList<PostMediaDto> Media,
    PostLocation? Location,
    IReadOnlyList<string> TaggedPetIds,
    IReadOnlyList<string> TaggedUserIds,
    IReadOnlyList<string> Hashtags,
    PostVisibility Visibility,
    int LikesCount,
    int CommentsCount,
    int SharesCount,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);

public class ReactToPostRequest
{
    [EnumDataType(typeof(ReactionType))]
    public required ReactionType Type { get; init; }

    // Informe para reagir em nome de um pet.
    public string? PetId { get; set; }
}

public class PostReactionSearchFilter
{
    [Range(1, 1_000_000)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public ReactionType? Type { get; set; }
    public string? UserId { get; set; }
    public string? PetId { get; set; }
}

public record PostReactionResponseDto(
    string Id,
    string PostId,
    string UserId,
    string? PetId,
    ReactionType Type,
    DateTime CreatedAt
);
