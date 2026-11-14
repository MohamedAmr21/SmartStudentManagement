using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartStudentManagement.DTOs.Auth;
using SmartStudentManagement.Services;

namespace SmartStudentManagement.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    [AllowAnonymous, HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        var result = await _auth.RegisterStudentAsync(dto);
        return result.Success ? Ok(result.Response) : BadRequest(new { error = result.Error });
    }

    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var result = await _auth.LoginAsync(dto);
        return result.Success ? Ok(result.Response) : Unauthorized(new { error = result.Error });
    }

    [Authorize, HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        UserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        Email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
        Roles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(x => x.Value)
    });
}
