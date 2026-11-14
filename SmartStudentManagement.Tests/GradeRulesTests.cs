using SmartStudentManagement.Services;
using Xunit;

namespace SmartStudentManagement.Tests;

public class GradeRulesTests
{
    [Theory]
    [InlineData(0, 100, true)]
    [InlineData(75, 100, true)]
    [InlineData(100, 100, true)]
    [InlineData(-1, 100, false)]
    [InlineData(101, 100, false)]
    [InlineData(10, 0, false)]
    [InlineData(10, -1, false)]
    public void IsValidScore_ValidatesScoreAgainstMaximum(decimal score, decimal maximum, bool expected)
    {
        Assert.Equal(expected, GradeRules.IsValidScore(score, maximum));
    }
}