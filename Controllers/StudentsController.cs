using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.DTOs.Students;
using SmartStudentManagement.Services;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/students")]
[Authorize]
public class StudentsController : ControllerBase
{
    private readonly IStudentService _students;
    public StudentsController(IStudentService students) => _students = students;

    [HttpGet, Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetAll() => Ok(await _students.GetAllAsync());

    [HttpGet("{id:int}"), Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> GetById(int id)
    {
        var student = await _students.GetByIdAsync(id);
        return student is null ? NotFound() : Ok(student);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateStudentDto dto)
    {
        var result = await _students.CreateAsync(dto);
        if (result.Student is null) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Student.StudentId }, result.Student);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, UpdateStudentDto dto) =>
        await _students.UpdateAsync(id, dto) ? NoContent() : NotFound();

    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) =>
        await _students.DeleteAsync(id) ? NoContent() : NotFound();

    [HttpGet("me"), Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var student = await _students.GetMeAsync(userId);
        return student is null ? NotFound(new { error = "Student profile not found." }) : Ok(student);
    }
}
