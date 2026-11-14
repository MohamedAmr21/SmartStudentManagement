using System.ComponentModel.DataAnnotations;

namespace SmartStudentManagement.DTOs.Admin;

public class UpdateUserRoleDto
{
    [Required] public string Role { get; set; } = string.Empty;
    public string? FirstName { get; set; }

    public string? LastName { get; set; }
}

public record AdminUserResponseDto(string Id, string? Email, bool IsActive, IList<string> Roles);
public record DashboardStatisticsDto(
    int TotalUsers,
    int ActiveStudents,
    int InactiveStudents,
    int ActiveTeachers,
    int InactiveTeachers,
    int TotalCourses,
    int ActiveEnrollments
);