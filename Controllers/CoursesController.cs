using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.DTOs.Courses;
using SmartStudentManagement.Services;
using System.Security.Claims;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/courses")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courses;
    public CoursesController(ICourseService courses) { _courses = courses; }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _courses.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var course = await _courses.GetByIdAsync(id);
        return course is null ? NotFound() : Ok(course);
    }


    [HttpGet("assigned")]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetAssignedCourses()
    {
        var isAdmin = User.IsInRole("Admin");

        var userId = isAdmin
            ? null
            : User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!isAdmin && userId is null)
            return Unauthorized();

        var courses = await _courses.GetAssignedCoursesAsync(
            userId,
            isAdmin
        );

        return Ok(courses);
    }

    [HttpGet("student/me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyStudentCourses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return Unauthorized();
        return Ok(await _courses.GetMyStudentCoursesAsync(userId));
    }

    [HttpGet("me")]
    [Authorize(Roles = "Teacher")]
    public async Task<IActionResult> GetMyCourses()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        return Ok(await _courses.GetMyCoursesAsync(userId));
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateCourseDto dto)
    {
        var result = await _courses.CreateAsync(dto);
        return result.Course is null ? BadRequest(new { error = result.Error }) : CreatedAtAction(nameof(GetById), new { id = result.Course.CourseId }, result.Course);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateCourseDto dto)
    {
        try { return await _courses.UpdateAsync(id, dto) ? NoContent() : NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    [HttpPost("{id:int}/teachers"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignTeacher(int id, AssignTeacherDto dto) =>
        await _courses.AssignTeacherAsync(id, dto.TeacherId) ? NoContent() : NotFound(new { error = "Course or teacher not found." });

    [HttpDelete("{id:int}/teachers/{teacherId:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> RemoveTeacher(int id, int teacherId) =>
        await _courses.RemoveTeacherAsync(id, teacherId) ? NoContent() : NotFound();

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) =>
        await _courses.DeleteAsync(id) ? NoContent() : NotFound();

}
