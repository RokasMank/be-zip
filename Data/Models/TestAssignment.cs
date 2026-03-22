namespace math4ktu_be.Data.Models;

public class TestAssignment
{
    public int Id { get; set; } // Primary key
    public string Title { get; set; }
    public string Description { get; set; }
    public int TestId { get; set; } // Foreign key to the Test
    public int Class { get; set; }
    public Test Test { get; set; } // Navigation property to the Test

    public List<Student> Students { get; set; } = new(); // List of assigned students

    public List<StudentTestSession> StudentsSessions { get; set; } = new();
    // Metadata for the assignment
    public bool IsPublished { get; set; } = false; // Is this assignment visible to students?
    public TestAssignmentStatus TestAssignmentStatus { get; set; }
    public DateTime? PublishDate { get; set; } // When was the test published?
    public DateTime? StartDate { get; set; } // When can students start the test?
    public DateTime? EndDate { get; set; } // When does the test close?

    // Assignment-specific settings
    public bool IsTimeLimited { get; set; } = false; // Overrides Test's time limit if true
    public TimeSpan? TimeLimit { get; set; } // Specific time limit for this assignment
}
