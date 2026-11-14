using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.DTOs.Enrollments;
using SmartStudentManagement.Services;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/enrollments")]
[Authorize]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollments;
    public EnrollmentsController(IEnrollmentService enrollments) => _enrollments = enrollments;

    [HttpGet,Authorize(Roles ="Admin,Teacher")]
    public async Task<IActionResult> GetAllEnrollment()
    {
        var result= await _enrollments.GetAllAsync();
        return Ok(result);
    }

    [HttpPost, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateEnrollmentDto dto)
    {
        var result = await _enrollments.CreateAsync(dto);
        return result.Enrollment is null ? BadRequest(new { error = result.Error }) : Ok(result.Enrollment);
    }


    [HttpGet("me"), Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine() =>
        Ok(await _enrollments.GetMineAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var isAdmin = User.IsInRole("Admin");
        var ok = await _enrollments.WithdrawAsync(id, User.FindFirstValue(ClaimTypes.NameIdentifier), isAdmin);
        return ok ? NoContent() : NotFound();
    }
}
