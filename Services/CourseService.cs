using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Courses;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface ICourseService
{
    Task<List<CourseResponseDto>> GetAllAsync();
    Task<CourseResponseDto?> GetByIdAsync(int id);
    Task<List<CourseResponseDto>> GetMyStudentCoursesAsync(string userId);
    Task<(CourseResponseDto? Course, string? Error)> CreateAsync(CreateCourseDto dto);
    Task<List<CourseWithTeachersResponseDto>> GetAssignedCoursesAsync(
    string? userId,
    bool isAdmin
);
    Task<List<CourseResponseDto>> GetMyCoursesAsync(string userId);
    Task<bool> UpdateAsync(int id, UpdateCourseDto dto);
    Task<bool> AssignTeacherAsync(int courseId, int teacherId);
    Task<bool> RemoveTeacherAsync(int courseId, int teacherId);
    Task<bool> DeleteAsync(int id);
}

public class CourseService : ICourseService
{
    private readonly ApplicationDbContext _db;
    public CourseService(ApplicationDbContext db) => _db = db;

    public async Task<List<CourseResponseDto>> GetAllAsync() =>
        await _db.Courses.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Title)
            .Select(x => new CourseResponseDto(x.CourseId, x.CourseCode, x.Title, x.Description, x.Credits, x.IsActive)).ToListAsync();

    public async Task<CourseResponseDto?> GetByIdAsync(int id) =>
        await _db.Courses.AsNoTracking().Where(x => x.CourseId == id && x.IsActive)
            .Select(x => new CourseResponseDto(x.CourseId, x.CourseCode, x.Title, x.Description, x.Credits, x.IsActive))
            .SingleOrDefaultAsync();
    
    public async Task<List<CourseResponseDto>> GetMyStudentCoursesAsync(
    string userId)
    {
        var student = await _db.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null)
            return new List<CourseResponseDto>();

        return await _db.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == student.StudentId &&
                e.Status == EnrollmentStatus.Active && e.Course.IsActive)
            .Select(e => new CourseResponseDto(
                e.Course.CourseId,
                e.Course.CourseCode,
                e.Course.Title,
                e.Course.Description,
                e.Course.Credits,
                e.Course.IsActive
            ))
            .Distinct()
            .ToListAsync();
    }
    

    public async Task<List<CourseWithTeachersResponseDto>> GetAssignedCoursesAsync(
    string? userId,
    bool isAdmin)
    {
        var query = _db.Courses.AsNoTracking().AsQueryable();

        // Teachers can only see courses assigned to them.
        if (!isAdmin)
        {
            if (userId is null)
                return new List<CourseWithTeachersResponseDto>();

            var teacher = await _db.Teachers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (teacher is null)
                return new List<CourseWithTeachersResponseDto>();

            query = query.Where(c =>
                c.CourseTeachers.Any(ct =>
                    ct.TeacherId == teacher.TeacherId));
        }

        return await query
            .OrderBy(c => c.Title)
            .Select(c => new CourseWithTeachersResponseDto(
                c.CourseId,
                c.CourseCode,
                c.Title,
                c.Description,
                c.Credits,
                c.IsActive,
                c.CourseTeachers
                    .Select(ct => new TeacherSummaryDto(
                        ct.Teacher.TeacherId,
                        ct.Teacher.FirstName,
                        ct.Teacher.LastName
                    ))
                    .ToList()
            ))
            .ToListAsync();
    }


   public async Task<List<CourseResponseDto>> GetMyCoursesAsync(string userId)
    {
        var teacher = await _db.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher is null)
            return new List<CourseResponseDto>();

        return await _db.CourseTeachers
            .AsNoTracking()
            .Where(ct => ct.TeacherId == teacher.TeacherId)
            .Select(ct => new CourseResponseDto(
                ct.Course.CourseId,
                ct.Course.CourseCode,
                ct.Course.Title,
                ct.Course.Description,
                ct.Course.Credits,
                ct.Course.IsActive
            ))
            .ToListAsync();
    }

    public async Task<(CourseResponseDto? Course, string? Error)> CreateAsync(CreateCourseDto dto)
    {
        if (await _db.Courses.AnyAsync(x => x.CourseCode.ToUpper() == dto.CourseCode.Trim().ToUpper()))
            return (null, "Course code already exists.");

        var course = new Course { CourseCode = dto.CourseCode.Trim().ToUpperInvariant(), Title = dto.Title.Trim(), Description = dto.Description, Credits = dto.Credits };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync();
        return (new CourseResponseDto(course.CourseId, course.CourseCode, course.Title, course.Description, course.Credits, course.IsActive), null);
    }

    public async Task<bool> UpdateAsync(int id, UpdateCourseDto dto)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return false;
        if (await _db.Courses.AnyAsync(x => x.CourseCode.ToUpper() == dto.CourseCode.Trim().ToUpper() && x.CourseId != id))
            throw new InvalidOperationException("Course code already exists.");
        course.CourseCode = dto.CourseCode.Trim().ToUpperInvariant();
        course.Title = dto.Title.Trim();
        course.Description = dto.Description;
        course.Credits = dto.Credits;
        course.IsActive = dto.IsActive;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AssignTeacherAsync(int courseId, int teacherId)
    {
        if (!await _db.Courses.AnyAsync(x => x.CourseId == courseId) ||
            !await _db.Teachers.AnyAsync(x => x.TeacherId == teacherId)) return false;
        if (await _db.CourseTeachers.AnyAsync(x => x.CourseId == courseId && x.TeacherId == teacherId)) return true;
        _db.CourseTeachers.Add(new CourseTeacher { CourseId = courseId, TeacherId = teacherId });
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveTeacherAsync(int courseId, int teacherId)
    {
        var assignment = await _db.CourseTeachers.FindAsync(courseId, teacherId);
        if (assignment is null) return false;
        _db.CourseTeachers.Remove(assignment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var course = await _db.Courses.FindAsync(id);
        if (course is null) return false;
        _db.Courses.Remove(course);
        await _db.SaveChangesAsync();
        return true;
    }
}
