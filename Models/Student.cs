using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        [Required] public string UserId { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
        [MaxLength(500)] public string? ProfilePhotoUrl { get; set; }
        public User User { get; set; } = null!;
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
    
}