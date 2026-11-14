using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class Teacher
    {
        public int TeacherId { get; set; }
        [Required] public string UserId { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
        public User User { get; set; } = null!;
        public ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>();
    }
    
}