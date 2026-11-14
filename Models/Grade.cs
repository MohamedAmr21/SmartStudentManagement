using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class Grade
    {
        public int GradeId { get; set; }
        public int AssessmentId { get; set; }
        public Assessment Assessment { get; set; } = null!;
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int GradedByTeacherId { get; set; }
        public Teacher GradedByTeacher { get; set; } = null!;
        public decimal Score { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
    
}