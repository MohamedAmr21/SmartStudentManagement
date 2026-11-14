using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartStudentManagement.DTOs.Students;
using SmartStudentManagement.Models;

namespace SmartStudentManagement.Services;

public interface IStudentService
{
    Task<List<StudentResponseDto>> GetAllAsync();
    Task<StudentResponseDto?> GetByIdAsync(int id);
    Task<(StudentResponseDto? Student, string? Error)> CreateAsync(CreateStudentDto dto);
    Task<bool> UpdateAsync(int id, UpdateStudentDto dto);
    Task<bool> DeleteAsync(int id);
    Task<StudentResponseDto?> GetMeAsync(string userId);
}

public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _users;

    public StudentService(ApplicationDbContext db, UserManager<User> users) { _db = db; _users = users; }

   
    
    public async Task<List<StudentResponseDto>> GetAllAsync()
    {
        var students = await (
            from s in _db.Students.AsNoTracking()
            join u in _db.Users on s.UserId equals u.Id
            join ur in _db.UserRoles on u.Id equals ur.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.NormalizedName == "STUDENT" && u.IsActive
            orderby s.LastName, s.FirstName
            select new StudentResponseDto(
                s.StudentId,
                s.UserId,
                u.Email!,
                s.FirstName,
                s.LastName,
                s.ProfilePhotoUrl
            )
        ).ToListAsync();

        return students;
    }



    public async Task<StudentResponseDto?> GetByIdAsync(int id) =>
        await _db.Students.AsNoTracking().Include(x => x.User)
            .Where(x => x.StudentId == id)
            .Select(x => new StudentResponseDto(x.StudentId, x.UserId, x.User.Email!, x.FirstName, x.LastName, x.ProfilePhotoUrl))
            .SingleOrDefaultAsync();

    public async Task<StudentResponseDto?> GetMeAsync(string userId) =>
        await _db.Students.AsNoTracking().Include(x => x.User)
            .Where(x => x.UserId == userId)
            .Select(x => new StudentResponseDto(x.StudentId, x.UserId, x.User.Email!, x.FirstName, x.LastName, x.ProfilePhotoUrl))
            .SingleOrDefaultAsync();

    public async Task<(StudentResponseDto? Student, string? Error)> CreateAsync(CreateStudentDto dto)
    {
        var user = new User { UserName = dto.Email, Email = dto.Email, IsActive = true };
        var created = await _users.CreateAsync(user, dto.TemporaryPassword);
        if (!created.Succeeded)
            return (null, string.Join("; ", created.Errors.Select(x => x.Description)));

        var roleResult = await _users.AddToRoleAsync(user, "Student");
        if (!roleResult.Succeeded)
        {
            await _users.DeleteAsync(user);
            return (null, "Could not assign Student role.");
        }

        var student = new Student { UserId = user.Id, FirstName = dto.FirstName, LastName = dto.LastName };
        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return (new StudentResponseDto(student.StudentId, student.UserId, dto.Email, student.FirstName, student.LastName, null), null);
    }

    public async Task<bool> UpdateAsync(int id, UpdateStudentDto dto)
    {
        var student = await _db.Students.FindAsync(id);
        if (student is null) return false;
        student.FirstName = dto.FirstName;
        student.LastName = dto.LastName;
        student.ProfilePhotoUrl = dto.ProfilePhotoUrl;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var student = await _db.Students.Include(x => x.Enrollments).SingleOrDefaultAsync(x => x.StudentId == id);
        if (student is null) return false;
        // Soft-deactivate account to preserve academic history.
        var user = await _users.FindByIdAsync(student.UserId);
        if (user is not null) { user.IsActive = false; await _users.UpdateAsync(user); }
        return true;
    }
}
