using System.ComponentModel.DataAnnotations;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.DTOs.Attendance;

public class MarkAttendanceItemDto
{
    [Range(1, int.MaxValue)] public int StudentId { get; set; }
    public AttendanceStatus Status { get; set; }
}

public class BulkAttendanceDto
{
    [Range(1, int.MaxValue)] public int CourseId { get; set; }
    public DateOnly AttendanceDate { get; set; }
    [Required, MinLength(1)] public List<MarkAttendanceItemDto> Records { get; set; } = [];
}

public record AttendanceResponseDto(int AttendanceId, int StudentId, int CourseId, DateOnly AttendanceDate, AttendanceStatus Status);
