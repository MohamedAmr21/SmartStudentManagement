using System.ComponentModel.DataAnnotations;

namespace SmartStudentManagement.DTOs.Courses;

public class CreateCourseDto
{
    [Required, MaxLength(30)] public string CourseCode { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string Title { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    [Range(1, 10)] public int Credits { get; set; } = 3;
}

public class UpdateCourseDto : CreateCourseDto
{
    public bool IsActive { get; set; } = true;
}

public record CourseResponseDto(int CourseId, string CourseCode, string Title, string? Description, int Credits, bool IsActive);
public class AssignTeacherDto { [Range(1, int.MaxValue)] public int TeacherId { get; set; } }


public record TeacherSummaryDto(
    int TeacherId,
    string FirstName,
    string LastName
);

public record CourseWithTeachersResponseDto(
    int CourseId,
    string CourseCode,
    string Title,
    string? Description,
    int Credits,
    bool IsActive,
    List<TeacherSummaryDto> Teachers
);
