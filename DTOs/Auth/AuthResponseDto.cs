namespace SmartStudentManagement.DTOs.Auth;

public record AuthResponseDto(string AccessToken, DateTime ExpiresAtUtc, string UserId, string Email, string Role);
