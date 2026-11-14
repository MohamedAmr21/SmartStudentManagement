
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Teachers;
using SmartStudentManagement.Models;
using System.Security.Claims;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/teachers")]
[Authorize]
public class TeachersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _users;

    public TeachersController(
        ApplicationDbContext db,
        UserManager<User> users)
    {
        _db = db;
        _users = users;
    }

    // GET: api/teachers
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<TeacherResponseDto>>> GetAll()
    {
        var teachers = await _db.Teachers
            .AsNoTracking()
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Select(t => new TeacherResponseDto(
                t.TeacherId,
                t.UserId,
                t.User.Email,
                t.FirstName,
                t.LastName
            ))
            .ToListAsync();

        return Ok(teachers);
    }

    // GET: api/teachers/1
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TeacherResponseDto>> GetById(int id)
    {
        var teacher = await _db.Teachers
            .AsNoTracking()
            .Where(t => t.TeacherId == id)
            .Select(t => new TeacherResponseDto(
                t.TeacherId,
                t.UserId,
                t.User.Email,
                t.FirstName,
                t.LastName
            ))
            .SingleOrDefaultAsync();

        if (teacher is null)
            return NotFound(new { error = "Teacher not found." });

        return Ok(teacher);
    }

    // GET: api/teachers/me
    [HttpGet("me")]
    [Authorize(Roles = "Teacher")]
    public async Task<ActionResult<TeacherResponseDto>> GetMe()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (userId is null)
            return Unauthorized();

        var teacher = await _db.Teachers
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .Select(t => new TeacherResponseDto(
                t.TeacherId,
                t.UserId,
                t.User.Email,
                t.FirstName,
                t.LastName
            ))
            .SingleOrDefaultAsync();

        if (teacher is null)
            return NotFound(new {
                error = "Teacher profile not found."
            });

        return Ok(teacher);
    }

    

    // PUT: api/teachers/1
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(
        int id,
        UpdateTeacherDto dto)
    {
        var teacher = await _db.Teachers
            .SingleOrDefaultAsync(t => t.TeacherId == id);

        if (teacher is null)
            return NotFound(new {
                error = "Teacher not found."
            });

        teacher.FirstName = dto.FirstName;
        teacher.LastName = dto.LastName;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/teachers/1
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var teacher = await _db.Teachers
            .Include(t => t.CourseTeachers)
            .SingleOrDefaultAsync(t => t.TeacherId == id);

        if (teacher is null)
            return NotFound(new {
                error = "Teacher not found."
            });

        if (teacher.CourseTeachers.Count > 0)
            return Conflict(new {
                error = "Remove this teacher's course assignments first."
            });

        _db.Teachers.Remove(teacher);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}
