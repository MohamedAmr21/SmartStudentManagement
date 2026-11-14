using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Admin;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface IAdminService
{
    Task<List<AdminUserResponseDto>> GetUsersAsync();
    Task<(bool Success, string? Error)> ChangeRoleAsync(
    string userId, UpdateUserRoleDto dto);
    Task<DashboardStatisticsDto> GetStatisticsAsync();
}

public class AdminService : IAdminService
{
     private static readonly string[] AllowedRoles = ["Admin", "Teacher", "Student"];
     private readonly RoleManager<IdentityRole> _roles;
    private readonly UserManager<User> _users;
    private readonly ApplicationDbContext _db;
    public AdminService(
    UserManager<User> users,
    ApplicationDbContext db,
    RoleManager<IdentityRole> roles)
{
    _users = users;
    _db = db;
    _roles = roles;
}

    public async Task<List<AdminUserResponseDto>> GetUsersAsync()
    {
        var users = await _users.Users.OrderBy(x => x.Email).ToListAsync();
        var result = new List<AdminUserResponseDto>();
        foreach (var user in users)
            result.Add(new AdminUserResponseDto(user.Id, user.Email, user.IsActive, await _users.GetRolesAsync(user)));
        return result;
    }

public async Task<(bool Success, string? Error)> ChangeRoleAsync(
    string userId, UpdateUserRoleDto dto)
{
    if (string.IsNullOrWhiteSpace(dto.Role) ||
        !AllowedRoles.Contains(dto.Role, StringComparer.OrdinalIgnoreCase))
    {
        return (false, "Invalid role.");
    }

    var targetRole = AllowedRoles.Single(
        x => x.Equals(dto.Role, StringComparison.OrdinalIgnoreCase));

    var user = await _users.FindByIdAsync(userId);

    if (user is null)
        return (false, "User not found.");

    if (!await _roles.RoleExistsAsync(targetRole))
        return (false, $"Role '{targetRole}' does not exist.");

    // Validate names before creating a profile
    if (targetRole == "Teacher" &&
        (string.IsNullOrWhiteSpace(dto.FirstName) ||
         string.IsNullOrWhiteSpace(dto.LastName)))
    {
        return (false, "FirstName and LastName are required for a Teacher.");
    }

    var oldRoles = await _users.GetRolesAsync(user);

    // Change the role only if necessary
    if (!oldRoles.Contains(targetRole))
    {
        var remove = await _users.RemoveFromRolesAsync(user, oldRoles);

        if (!remove.Succeeded)
        {
            return (false, string.Join("; ",
                remove.Errors.Select(e => e.Description)));
        }

        var add = await _users.AddToRoleAsync(user, targetRole);

        if (!add.Succeeded)
        {
            return (false, string.Join("; ",
                add.Errors.Select(e => e.Description)));
        }
    }

    // Ensure a Teacher profile exists, even if the role already existed
    if (targetRole == "Teacher")
    {
        var teacherExists = await _db.Teachers
            .AnyAsync(t => t.UserId == user.Id);

        if (!teacherExists)
        {
            _db.Teachers.Add(new Teacher
            {
                UserId = user.Id,
                FirstName = dto.FirstName!.Trim(),
                LastName = dto.LastName!.Trim()
            });
        }
    }

    // Save any profile changes
    await _db.SaveChangesAsync();

    return (true, null);
}
   
    
    public async Task<DashboardStatisticsDto> GetStatisticsAsync()
{
    var studentRole = await _roles.FindByNameAsync("Student");
    var teacherRole = await _roles.FindByNameAsync("Teacher");

    var activeStudentCount = studentRole is null
        ? 0
        : await (
            from ur in _db.UserRoles
            join u in _db.Users on ur.UserId equals u.Id
            where ur.RoleId == studentRole.Id && u.IsActive
            select u.Id
        ).CountAsync();

    var inactiveStudentCount = studentRole is null
        ? 0
        : await (
            from ur in _db.UserRoles
            join u in _db.Users on ur.UserId equals u.Id
            where ur.RoleId == studentRole.Id && !u.IsActive
            select u.Id
        ).CountAsync();

    var activeTeacherCount = teacherRole is null
        ? 0
        : await (
            from ur in _db.UserRoles
            join u in _db.Users on ur.UserId equals u.Id
            where ur.RoleId == teacherRole.Id && u.IsActive
            select u.Id
        ).CountAsync();

    var inactiveTeacherCount = teacherRole is null
        ? 0
        : await (
            from ur in _db.UserRoles
            join u in _db.Users on ur.UserId equals u.Id
            where ur.RoleId == teacherRole.Id && !u.IsActive
            select u.Id
        ).CountAsync();

    return new DashboardStatisticsDto(
        await _users.Users.CountAsync(),
        activeStudentCount,
        inactiveStudentCount,
        activeTeacherCount,
        inactiveTeacherCount,
        await _db.Courses.CountAsync(),
        await _db.Enrollments.CountAsync(
            e => e.Status == EnrollmentStatus.Active)
    );
}


}