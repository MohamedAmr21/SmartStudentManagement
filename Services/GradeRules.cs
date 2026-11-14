namespace SmartStudentManagement.Services;

public static class GradeRules
{
    public static bool IsValidScore(decimal score, decimal maximum) =>
        maximum > 0 && score >= 0 && score <= maximum;
}
