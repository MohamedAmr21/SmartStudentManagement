using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Attendance;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface IAttendanceService
{
    Task<(bool Success, string? Error)> MarkBulkAsync(string userId, BulkAttendanceDto dto);
    Task<List<AttendanceResponseDto>> GetMineAsync(string userId);
    Task<List<AttendanceResponseDto>?> GetCourseAsync(int courseId, int teacherId);
    Task<List<AttendanceResponseDto>?> GetCourseForAdminAsync(
        int courseId
    );
}

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _db;
    public AttendanceService(ApplicationDbContext db) => _db = db;

    public async Task<(bool Success, string? Error)> MarkBulkAsync(string userId, BulkAttendanceDto dto)
    {
        var teacher = await ServiceHelpers.GetTeacherAsync(_db, userId);
        if (teacher is null) return (false, "Teacher profile not found.");
        if (!await ServiceHelpers.TeachesCourseAsync(_db, teacher.TeacherId, dto.CourseId))
            return (false, "Teacher is not assigned to this course.");
        if (dto.Records.Select(x => x.StudentId).Distinct().Count() != dto.Records.Count)
            return (false, "The request contains duplicate student IDs.");

        foreach (var item in dto.Records)
        {
            if (!await _db.Enrollments.AnyAsync(x => x.StudentId == item.StudentId && x.CourseId == dto.CourseId && x.Status == EnrollmentStatus.Active))
                return (false, $"Student {item.StudentId} is not actively enrolled in this course.");
            var existing = await _db.Attendances.SingleOrDefaultAsync(x =>
                x.StudentId == item.StudentId && x.CourseId == dto.CourseId && x.AttendanceDate == dto.AttendanceDate);
            if (existing is not null)
            {
                existing.Status = item.Status;
                existing.MarkedByTeacherId = teacher.TeacherId;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.Attendances.Add(new Attendance
                {
                    StudentId = item.StudentId, CourseId = dto.CourseId,
                    AttendanceDate = dto.AttendanceDate, Status = item.Status,
                    MarkedByTeacherId = teacher.TeacherId
                });
            }
        }
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<List<AttendanceResponseDto>> GetMineAsync(string userId) =>
        await _db.Attendances.AsNoTracking().Where(x => x.Student.UserId == userId)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new AttendanceResponseDto(x.AttendanceId, x.StudentId, x.CourseId, x.AttendanceDate, x.Status))
            .ToListAsync();

    public async Task<List<AttendanceResponseDto>?> GetCourseAsync(int courseId, int teacherId)
    {
        if (!await ServiceHelpers.TeachesCourseAsync(_db, teacherId, courseId)) return null;
        return await _db.Attendances.AsNoTracking().Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new AttendanceResponseDto(x.AttendanceId, x.StudentId, x.CourseId, x.AttendanceDate, x.Status))
            .ToListAsync();
    }


    public async Task<List<AttendanceResponseDto>?> GetCourseForAdminAsync(
        int courseId)
    {
        // Verify that the course exists.
        var courseExists = await _db.Courses
            .AnyAsync(x => x.CourseId == courseId);

        if (!courseExists)
            return null;

        return await _db.Attendances
            .AsNoTracking()
            .Where(x => x.CourseId == courseId)
            .OrderByDescending(x => x.AttendanceDate)
            .Select(x => new AttendanceResponseDto(
                x.AttendanceId,
                x.StudentId,
                x.CourseId,
                x.AttendanceDate,
                x.Status
            ))
            .ToListAsync();
    }

}
