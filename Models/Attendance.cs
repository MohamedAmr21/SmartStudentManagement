using System.ComponentModel.DataAnnotations;


namespace SmartStudentManagement.Models
{
    public class Attendance
    {
        public int AttendanceId { get; set; }
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;
        public DateOnly AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public int MarkedByTeacherId { get; set; }
        public Teacher MarkedByTeacher { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
       
    }

    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        Excused
    }
    
}