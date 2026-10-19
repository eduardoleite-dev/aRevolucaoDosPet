namespace aRevolucaoDosPet.DTOs;

public record UserSettingsResponseDto(
    string Id,
    string UserId,
    NotificationSettingsDto Notifications,
    PrivacySettingsDto Privacy,
    string Theme,
    string Language,
    DateTime CreatedAt,
    DateTime UpdatedAt
);