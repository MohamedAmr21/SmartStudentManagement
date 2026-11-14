using System.ComponentModel.DataAnnotations;

namespace SmartStudentManagement.DTOs.Enrollments;

public class CreateEnrollmentDto
{
    [Range(1, int.MaxValue)] public int StudentId { get; set; }
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
}

public record EnrollmentResponseDto(int EnrollmentId, int StudentId, int CourseId, DateTime EnrolledAt, string Status);
