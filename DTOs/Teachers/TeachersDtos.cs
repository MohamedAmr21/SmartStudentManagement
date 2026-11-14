using System.ComponentModel.DataAnnotations;

namespace SmartStudentManagement.DTOs.Teachers;



public class UpdateTeacherDto
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;
    
}

public record TeacherResponseDto( int TeacherId,
    string UserId,
    string? Email,
    string FirstName,
    string LastName);
