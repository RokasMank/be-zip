namespace math4ktu_be.Data.Models;

public class Test
{
    public int Id { get; set; } // Primary key
    public string Title { get; set; } = string.Empty; // Test title
    public string Description { get; set; } = string.Empty; // Test instructions
    public TimeSpan? DefaultTimeLimit { get; set; } // Default time limit for the test
    public List<TestQuestion> TestQuestions { get; set; } = new();

    public int TotalPoints => TestQuestions.Sum(tq => tq.Points);
    public bool Published {  get; set; }
}
