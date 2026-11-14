using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.Models;
using SmartStudentManagement.DTOs.Grades;
using SmartStudentManagement.Services;
using Microsoft.EntityFrameworkCore;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class GradesController : ControllerBase
{
    private readonly IGradeService _grades;
    private readonly ApplicationDbContext _db;
    public GradesController(IGradeService grades, ApplicationDbContext db) { _grades = grades; _db = db; }

    [HttpPost("courses/{courseId:int}/assessments"), Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateAssessment(int courseId, CreateAssessmentDto dto)
    {
        var teacher = await ServiceHelpers.GetTeacherAsync(_db, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (teacher is null) return Forbid();
        var result = await _grades.CreateAssessmentAsync(courseId, teacher.TeacherId, dto);
        return result.Assessment is null ? Forbid() : Ok(result.Assessment);
    }

    [HttpPost("grades"), Authorize(Roles = "Teacher")]
    public async Task<IActionResult> CreateGrade(CreateGradeDto dto)
    {
        var teacher = await ServiceHelpers.GetTeacherAsync(_db, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (teacher is null) return Forbid();
        var result = await _grades.CreateGradeAsync(teacher.TeacherId, dto);
        if (result.Grade is not null) return Ok(result.Grade);
        return result.Error?.Contains("not assigned", StringComparison.OrdinalIgnoreCase) == true ? Forbid() : BadRequest(new { error = result.Error });
    }


    // GET: /api/courses/1/assessments
    [HttpGet("courses/{courseId:int}/assessments")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetCourseAssessments(int courseId)
    {
        if (!await _db.Courses.AnyAsync(c => c.CourseId == courseId))
            return NotFound(new { message = "Course not found." });

        // Admin can view assessments for any course.
        if (!User.IsInRole("Admin"))
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId is null)
                return Unauthorized();

            var teacher = await ServiceHelpers.GetTeacherAsync(_db, userId);

            if (teacher is null)
                return Forbid();

            // Teacher can view assessments only for assigned courses.
            var isAssigned = await _db.CourseTeachers.AnyAsync(ct =>
                ct.CourseId == courseId &&
                ct.TeacherId == teacher.TeacherId);

            if (!isAssigned)
                return Forbid();
        }

        var assessments = await _db.Assessments
            .AsNoTracking()
            .Where(a => a.CourseId == courseId)
            .Select(a => new AssessmentDetailsDto(
                a.AssessmentId,
                a.Title,
                a.MaxScore,
                a.CourseId,
                a.Course.Title
            ))
            .ToListAsync();

        return Ok(assessments);
    }

    [HttpGet("grades/me"), Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine() =>
        Ok(await _grades.GetMineAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));


    [HttpGet("courses/{courseId:int}/grades")]
    [Authorize(Roles = "Admin,Teacher")]

    public async Task<IActionResult> GetCourseGrades(int courseId)
    {
        // Admin can view grades for any course
        if (User.IsInRole("Admin"))
        {
            var adminGrades = await _grades.GetCourseGradesAsync(
                courseId,
                teacherId: null); // Requires the service to support nullable teacherId

            return adminGrades is null
                ? NotFound(new { message = "Course not found." })
                : Ok(adminGrades);
        }

        // Teachers can view grades only for their assigned courses
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var teacher = await ServiceHelpers.GetTeacherAsync(_db, userId);

        if (teacher is null)
            return Forbid();

        var grades = await _grades.GetCourseGradesAsync(
            courseId, teacher.TeacherId);

        return grades is null
            ? StatusCode(403, new
            {
                message = "Access denied. You can only view grades for courses assigned to you."
            })
            : Ok(grades);
    }


    [HttpPut("grades/{id:int}"), Authorize(Roles = "Teacher")]
    public async Task<IActionResult> UpdateGrade(int id, UpdateGradeDto dto)
    {
        var teacher = await ServiceHelpers.GetTeacherAsync(_db, User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (teacher is null) return Forbid();
        var result = await _grades.UpdateGradeAsync(id, teacher.TeacherId, dto);
        if (result.Success) return NoContent();
        return result.Error == "Grade not found." ? NotFound() :
            result.Error?.Contains("not assigned", StringComparison.OrdinalIgnoreCase) == true ? Forbid() :
            BadRequest(new { error = result.Error });
    }
}
