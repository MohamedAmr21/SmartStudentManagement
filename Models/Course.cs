using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class Course
    {
        public int CourseId { get; set; }
        [Required, MaxLength(30)] public string CourseCode { get; set; } = string.Empty;
        [Required, MaxLength(150)] public string Title { get; set; } = string.Empty;
        [MaxLength(1000)] public string? Description { get; set; }
        public int Credits { get; set; } = 3;
        public bool IsActive { get; set; } = true;
        public ICollection<CourseTeacher> CourseTeachers { get; set; } = new List<CourseTeacher>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Assessment> Assessments { get; set; } = new List<Assessment>();
       
    }
    
}