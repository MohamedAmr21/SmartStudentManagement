using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public static class ServiceHelpers
{
    public static string? CurrentUserId(ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.NameIdentifier);

    public static async Task<Teacher?> GetTeacherAsync(ApplicationDbContext db, string userId) =>
        await db.Teachers.SingleOrDefaultAsync(x => x.UserId == userId);

    public static async Task<Student?> GetStudentAsync(ApplicationDbContext db, string userId) =>
        await db.Students.SingleOrDefaultAsync(x => x.UserId == userId);

    public static Task<bool> TeachesCourseAsync(ApplicationDbContext db, int teacherId, int courseId) =>
        db.CourseTeachers.AnyAsync(x => x.TeacherId == teacherId && x.CourseId == courseId);

    
}
