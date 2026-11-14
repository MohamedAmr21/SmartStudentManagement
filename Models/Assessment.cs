using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class Assessment
    {
        public int AssessmentId { get; set; }
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        [Required, MaxLength(150)] public string Title { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public DateTime AssessmentDate { get; set; }
        public ICollection<Grade> Grades { get; set; } = new List<Grade>();
       
    }
    
}