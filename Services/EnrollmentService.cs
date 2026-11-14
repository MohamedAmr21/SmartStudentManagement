using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Enrollments;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface IEnrollmentService
{
    Task<(EnrollmentResponseDto? Enrollment, string? Error)> CreateAsync(CreateEnrollmentDto dto);
    Task<List<EnrollmentResponseDto>> GetAllAsync();
    Task<List<EnrollmentResponseDto>> GetMineAsync(string userId);
    Task<bool> WithdrawAsync(int enrollmentId, string? currentUserId, bool isAdmin);

}

public class EnrollmentService : IEnrollmentService
{
    private readonly ApplicationDbContext _db;
    public EnrollmentService(ApplicationDbContext db) => _db = db;

    public async Task<(EnrollmentResponseDto? Enrollment, string? Error)> CreateAsync(CreateEnrollmentDto dto)
    {
        if (!await _db.Students.AnyAsync(x => x.StudentId == dto.StudentId)) return (null, "Student not found.");
        if (!await _db.Courses.AnyAsync(x => x.CourseId == dto.CourseId && x.IsActive)) return (null, "Active course not found.");
        if (await _db.Enrollments.AnyAsync(x => x.StudentId == dto.StudentId && x.CourseId == dto.CourseId))
            return (null, "Student already has an enrollment record for this course.");

        var e = new Enrollment { StudentId = dto.StudentId, CourseId = dto.CourseId };
        _db.Enrollments.Add(e);
        await _db.SaveChangesAsync();
        return (new EnrollmentResponseDto(e.EnrollmentId, e.StudentId, e.CourseId, e.EnrolledAt, e.Status.ToString()), null);
    }
    public async Task<List<EnrollmentResponseDto>> GetAllAsync() =>
        await _db.Enrollments.AsNoTracking()
            .OrderByDescending(x => x.EnrolledAt)
            .Select(x => new EnrollmentResponseDto(x.EnrollmentId, x.StudentId, x.CourseId, x.EnrolledAt, x.Status.ToString()))
            .ToListAsync();

    public async Task<List<EnrollmentResponseDto>> GetMineAsync(string userId) =>
        await _db.Enrollments.AsNoTracking().Where(x => x.Student.UserId == userId)
            .OrderByDescending(x => x.EnrolledAt)
            .Select(x => new EnrollmentResponseDto(x.EnrollmentId, x.StudentId, x.CourseId, x.EnrolledAt, x.Status.ToString()))
            .ToListAsync();

    public async Task<bool> WithdrawAsync(int enrollmentId, string? currentUserId, bool isAdmin)
    {
        var e = await _db.Enrollments.Include(x => x.Student).SingleOrDefaultAsync(x => x.EnrollmentId == enrollmentId);
        if (e is null || (!isAdmin && e.Student.UserId != currentUserId)) return false;
        e.Status = EnrollmentStatus.Withdrawn;
        await _db.SaveChangesAsync();
        return true;
    }
}
