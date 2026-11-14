using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.Models;
using SmartStudentManagement.DTOs.Attendance;
using SmartStudentManagement.Services;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendance;
    private readonly ApplicationDbContext _db;
    public AttendanceController(IAttendanceService attendance, ApplicationDbContext db) { _attendance = attendance; _db = db; }

    [HttpPost("bulk"), Authorize(Roles = "Teacher")]
    public async Task<IActionResult> MarkBulk(BulkAttendanceDto dto)
    {
        var result = await _attendance.MarkBulkAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, dto);
        if (result.Success) return Ok(new { message = "Attendance saved." });
        return result.Error?.Contains("not assigned", StringComparison.OrdinalIgnoreCase) == true ? Forbid() : BadRequest(new { error = result.Error });
    }

    [HttpGet("me"), Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine() =>
        Ok(await _attendance.GetMineAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

    
[HttpGet("courses/{courseId:int}")]
[Authorize(Roles = "Admin,Teacher")]
public async Task<IActionResult> GetCourse(int courseId)
{
    // Admin can view attendance for any course.
    if (User.IsInRole("Admin"))
    {
        var adminResult = await _attendance.GetCourseForAdminAsync(courseId);

        return adminResult is null
            ? NotFound(new { error = "Course not found." })
            : Ok(adminResult);
    }

    // Teacher can only view attendance for assigned courses.
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

    if (string.IsNullOrEmpty(userId))
        return Unauthorized();

    var teacher = await ServiceHelpers.GetTeacherAsync(_db, userId);

    if (teacher is null)
        return Forbid();

    var result = await _attendance.GetCourseAsync(
        courseId,
        teacher.TeacherId
    );

    return result is null
        ? Forbid()
        : Ok(result);
}

}
