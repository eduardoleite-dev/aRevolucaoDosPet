namespace aRevolucaoDosPet.DTOs;

public record UserResponseDto(
    string Id,
    string Name,
    string Email,
    string? AvatarUrl,
    string? Bio,
    string? Phone,
    string? City,
    string? State,
    string Country,
    Models.UserLocation? Location,
    int AccessLevel,
    string Status,
    DateTime? EmailVerifiedAt,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt
);