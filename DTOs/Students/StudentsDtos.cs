using System.ComponentModel.DataAnnotations;

namespace SmartStudentManagement.DTOs.Students;

public class CreateStudentDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string TemporaryPassword { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(500)] public string? ProfilePhotoUrl { get; set; }
}

public class UpdateStudentDto
{
    public string Role { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(500)] public string? ProfilePhotoUrl { get; set; }
}

public record StudentResponseDto(int StudentId, string UserId, string Email, string FirstName, string LastName, string? ProfilePhotoUrl);
