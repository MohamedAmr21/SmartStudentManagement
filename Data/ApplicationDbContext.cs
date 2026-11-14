
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SmartStudentManagement.Models
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)  {}

        
        public DbSet<Student> Students { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<CourseTeacher> CourseTeachers { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Assessment> Assessments { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
       


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Composite primary key for CourseTeacher
        builder.Entity<CourseTeacher>()
            .HasKey(ct => new { ct.CourseId, ct.TeacherId });

        // Configure decimal precision
        builder.Entity<Assessment>()
            .Property(a => a.MaxScore)
            .HasPrecision(10, 2);

        builder.Entity<Grade>()
            .Property(g => g.Score)
            .HasPrecision(10, 2);

        // Prevent multiple cascade paths
        builder.Entity<Attendance>()
            .HasOne(a => a.MarkedByTeacher)
            .WithMany()
            .HasForeignKey(a => a.MarkedByTeacherId)
            .OnDelete(DeleteBehavior.NoAction);

        // Prevent multiple cascade paths
        builder.Entity<Grade>()
        .HasOne(g => g.GradedByTeacher)
        .WithMany()
        .HasForeignKey(g => g.GradedByTeacherId)
        .OnDelete(DeleteBehavior.NoAction);
    }


    }

}