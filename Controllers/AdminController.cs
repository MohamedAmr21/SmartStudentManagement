using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.DTOs.Admin;
using SmartStudentManagement.Services;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    public AdminController(IAdminService admin) => _admin = admin;

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers() => Ok(await _admin.GetUsersAsync());

    [HttpPut("users/{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, UpdateUserRoleDto dto)
    {
        var result = await _admin.ChangeRoleAsync(id, dto);
        if (result.Success) return NoContent();
        var status = result.Error == "User not found." ? StatusCodes.Status404NotFound :
            result.Error?.Contains("history", StringComparison.OrdinalIgnoreCase) == true ||
            result.Error?.Contains("academic records", StringComparison.OrdinalIgnoreCase) == true ||
            result.Error?.Contains("assignments", StringComparison.OrdinalIgnoreCase) == true
                ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
        return StatusCode(status, new { error = result.Error });
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics() => Ok(await _admin.GetStatisticsAsync());
}
