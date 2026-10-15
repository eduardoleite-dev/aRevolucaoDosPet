namespace aRevolucaoDosPet.DTOs;

public record LoginResponseDto(
    string Token,
    DateTime ExpiresAt,
    string Username,
    string UserId
);