using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SmartStudentManagement.DTOs.Auth;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResponseDto? Response)> RegisterStudentAsync(RegisterRequestDto dto);
    Task<(bool Success, string? Error, AuthResponseDto? Response)> LoginAsync(LoginRequestDto dto);
}

public class AuthService : IAuthService
{
    private readonly UserManager<User> _users;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserManager<User> users, IConfiguration config, ApplicationDbContext db, ILogger<AuthService> logger)
    {
        _users = users;
        _config = config;
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Response)> RegisterStudentAsync(RegisterRequestDto dto)
    {
        var user = new User
        {
            UserName = dto.Email,
            Email = dto.Email,
            IsActive = true
        };

        var create = await _users.CreateAsync(user, dto.Password);
        if (!create.Succeeded)
        {
            _logger.LogWarning("Student registration failed for email {Email}; Identity returned {ErrorCount} validation errors", dto.Email, create.Errors.Count());
            return (false, string.Join("; ", create.Errors.Select(x => x.Description)), null);
        }

        var roleResult = await _users.AddToRoleAsync(user, "Student");
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            return (false, "Could not assign Student role.", null);
        }

        var student = new Student
        {
            UserId = user.Id,
            FirstName = dto.FirstName,
            LastName = dto.LastName
        };

        _db.Students.Add(student);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch
        {
            await _users.DeleteAsync(user);
            throw;
        }

        return (true, null, await CreateTokenAsync(user, "Student"));
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Response)> LoginAsync(LoginRequestDto dto)
    {
        var user = await _users.FindByEmailAsync(dto.Email);
        if (user is null || !user.IsActive || !await _users.CheckPasswordAsync(user, dto.Password))
        {
            _logger.LogWarning("Login attempt rejected for email {Email}", dto.Email);
            return (false, "Invalid credentials or inactive account.", null);
        }

        var roles = await _users.GetRolesAsync(user);
        if (roles.Count == 0)
            return (false, "The account has no assigned role.", null);

        _logger.LogInformation("User {UserId} authenticated successfully", user.Id);
        return (true, null, await CreateTokenAsync(user, roles[0]));
    }

    private async Task<AuthResponseDto> CreateTokenAsync(User user, string role)
    {
        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;
        var key = _config["Jwt:SecurityKey"]!;
        var expires = DateTime.UtcNow.AddMinutes(
            double.TryParse(_config["Jwt:ExpiresMinutes"], out var minutes) ? minutes : 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? ""),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: expires, signingCredentials: credentials);

        return new AuthResponseDto(new JwtSecurityTokenHandler().WriteToken(token), expires, user.Id, user.Email ?? "", role);
    }
}
