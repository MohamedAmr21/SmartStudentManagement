using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Grades;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface IGradeService
{
    Task<(AssessmentResponseDto? Assessment, string? Error)> CreateAssessmentAsync(int courseId, int teacherId, CreateAssessmentDto dto);
    Task<(GradeResponseDto? Grade, string? Error)> CreateGradeAsync(int teacherId, CreateGradeDto dto);
    Task<List<GradeResponseDto>> GetMineAsync(string userId);
    Task<List<GradeResponseDto>?> GetCourseGradesAsync(
    int courseId, int? teacherId);    Task<(bool Success, string? Error)> UpdateGradeAsync(int gradeId, int teacherId, UpdateGradeDto dto);
}

public class GradeService : IGradeService
{
    private readonly ApplicationDbContext _db;
    public GradeService(ApplicationDbContext db) => _db = db;

    public async Task<(AssessmentResponseDto? Assessment, string? Error)> CreateAssessmentAsync(int courseId, int teacherId, CreateAssessmentDto dto)
    {
        if (!await ServiceHelpers.TeachesCourseAsync(_db, teacherId, courseId)) return (null, "Teacher is not assigned to this course.");
        if (dto.MaxScore <= 0) return (null, "Assessment maximum score must be greater than zero.");
        var assessment = new Assessment { CourseId = courseId, Title = dto.Title.Trim(), MaxScore = dto.MaxScore, AssessmentDate = dto.AssessmentDate };
        _db.Assessments.Add(assessment);
        await _db.SaveChangesAsync();
        return (new AssessmentResponseDto(assessment.AssessmentId, courseId, assessment.Title, assessment.MaxScore, assessment.AssessmentDate), null);
    }

    public async Task<(GradeResponseDto? Grade, string? Error)> CreateGradeAsync(int teacherId, CreateGradeDto dto)
    {
        var assessment = await _db.Assessments.Include(x => x.Course).SingleOrDefaultAsync(x => x.AssessmentId == dto.AssessmentId);
        if (assessment is null) return (null, "Assessment not found.");
        if (!await ServiceHelpers.TeachesCourseAsync(_db, teacherId, assessment.CourseId)) return (null, "Teacher is not assigned to this course.");
        if (!GradeRules.IsValidScore(dto.Score, assessment.MaxScore)) return (null, "Score must be between zero and the assessment maximum.");
        if (!await _db.Enrollments.AnyAsync(x => x.StudentId == dto.StudentId && x.CourseId == assessment.CourseId && x.Status == EnrollmentStatus.Active))
            return (null, "Student is not actively enrolled in this course.");
        if (await _db.Grades.AnyAsync(x => x.AssessmentId == dto.AssessmentId && x.StudentId == dto.StudentId))
            return (null, "A grade already exists for this student and assessment.");

        var grade = new Grade { AssessmentId = dto.AssessmentId, StudentId = dto.StudentId, GradedByTeacherId = teacherId, Score = dto.Score };
        _db.Grades.Add(grade);
        await _db.SaveChangesAsync();
        return (new GradeResponseDto(grade.GradeId, grade.AssessmentId, grade.StudentId, assessment.Title, grade.Score, assessment.MaxScore, grade.CreatedAt, grade.UpdatedAt), null);
    }

    public async Task<List<GradeResponseDto>> GetMineAsync(string userId) =>
        await _db.Grades.AsNoTracking().Where(x => x.Student.UserId == userId)
            .OrderBy(x => x.Assessment.AssessmentDate)
            .Select(x => new GradeResponseDto(x.GradeId, x.AssessmentId, x.StudentId, x.Assessment.Title, x.Score, x.Assessment.MaxScore, x.CreatedAt, x.UpdatedAt))
            .ToListAsync();

    public async Task<List<GradeResponseDto>?> GetCourseGradesAsync(
    int courseId, int? teacherId)
    {
        // Check that the course exists
        var courseExists = await _db.Courses
            .AnyAsync(c => c.CourseId == courseId);

        if (!courseExists)
            return null;

        // If teacherId is provided, verify course assignment.
        // If null, the caller is expected to have authorized an Admin.
        if (teacherId.HasValue &&
            !await ServiceHelpers.TeachesCourseAsync(
                _db, teacherId.Value, courseId))
        {
            return null;
        }

        return await _db.Grades
            .AsNoTracking()
            .Where(g => g.Assessment.CourseId == courseId)
            .Select(g => new GradeResponseDto(
                g.GradeId,
                g.AssessmentId,
                g.StudentId,
                g.Assessment.Title,
                g.Score,
                g.Assessment.MaxScore,
                g.CreatedAt,
                g.UpdatedAt))
            .ToListAsync();
    }
    public async Task<(bool Success, string? Error)> UpdateGradeAsync(int gradeId, int teacherId, UpdateGradeDto dto)
    {
        var grade = await _db.Grades.Include(x => x.Assessment).SingleOrDefaultAsync(x => x.GradeId == gradeId);
        if (grade is null) return (false, "Grade not found.");
        if (!await ServiceHelpers.TeachesCourseAsync(_db, teacherId, grade.Assessment.CourseId)) return (false, "Teacher is not assigned to this course.");
        if (!GradeRules.IsValidScore(dto.Score, grade.Assessment.MaxScore)) return (false, "Score must be between zero and the assessment maximum.");
        grade.Score = dto.Score;
        grade.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }
}
