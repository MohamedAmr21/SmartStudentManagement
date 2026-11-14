using System.ComponentModel.DataAnnotations;

namespace SmartStudentManagement.DTOs.Grades;

public class CreateAssessmentDto
{
    [Required, MaxLength(150)] public string Title { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "100000")] public decimal MaxScore { get; set; }
    public DateTime AssessmentDate { get; set; }
}

public class CreateGradeDto
{
    [Range(1, int.MaxValue)] public int AssessmentId { get; set; }
    [Range(1, int.MaxValue)] public int StudentId { get; set; }
    [Range(typeof(decimal), "0", "100000")] public decimal Score { get; set; }
}

public class UpdateGradeDto
{
    [Range(typeof(decimal), "0", "100000")] public decimal Score { get; set; }
}




public record AssessmentDetailsDto(
    int AssessmentId,
    string Title,
    decimal MaxScore,
    int CourseId,
    string CourseTitle
);

public record GradeResponseDto(int GradeId, int AssessmentId, int StudentId, string AssessmentTitle, decimal Score, decimal MaxScore, DateTime CreatedAt, DateTime? UpdatedAt);
public record AssessmentResponseDto(int AssessmentId, int CourseId, string Title, decimal MaxScore, DateTime AssessmentDate);
