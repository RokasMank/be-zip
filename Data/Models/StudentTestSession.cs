namespace math4ktu_be.Data.Models;

public class StudentTestSession
{
    public int Id { get; set; } // Primary key

    public int TestId { get; set; } // Foreign key to the Test entity
    public Test Test { get; set; } = null!; // Navigation property for Test

    public int StudentId { get; set; } // Foreign key to the Student entity
    public Student Student { get; set; } = null!; // Navigation property for Student
    public TestAssignment TestAssignment { get; set; } = null!;
    public int TestAssignmentId { get; set; }
    public List<StudentAnswer> Answers { get; set; } = []; // One-to-many relationship with StudentAnswer

    public int Score { get; set; } // Calculated score after submission
    public StudentSessionStatus SessionStatus {get; set;}
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set;}
}
