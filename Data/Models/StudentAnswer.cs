namespace math4ktu_be.Data.Models;

public class StudentAnswer
{
    public int Id { get; set; } // Primary key
    public int StudentTestSessionId { get; set; } // Foreign key to StudentTest
    public StudentTestSession StudentTest { get; set; } = null!; // Navigation property for StudentTest
    public int QuestionId { get; set; } // Foreign key to the Question entity
    public Question Question { get; set; } = null!; // Navigation property for Question
    public List<string> ProvidedAnswers { get; set; } = new(); // List of selected options by the student
    [Obsolete]
    public bool IsCorrect { get; set; } // Indicates if the provided answer is correct
    public double PointsEarned { get; set; }
    public DateTime AnsweredAt { get; set; } // Timestamp for when the answer was provided
}