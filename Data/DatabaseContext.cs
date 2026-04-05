namespace math4ktu_be.Data;

using math4ktu_be.Data.Enums;
using math4ktu_be.Data.Models;
using Microsoft.EntityFrameworkCore;

public class DatabaseContext : DbContext
{
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    {
    }

    public DbSet<Student> Students { get; set; }
    public DbSet<Administrator> Administrators { get; set; }
    public DbSet<Question> Questions { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<TestQuestion> TestQuestions { get; set; }
    public DbSet<TestAssignment> TestAssignments { get; set; }
    public DbSet<StudentTestSession> StudentTestSessions { get; set; }
    public DbSet<StudentAnswer> StudentAnswers { get; set; }
    public DbSet<Role> Roles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = 1, Name = "Admin" },
            new Role { Id = 2, Name = "Student" }
        );

        // Configure base Question class and inheritance mapping
        modelBuilder.Entity<Question>()
            .HasDiscriminator<QuestionType>("QuestionType")
            .HasValue<OpenEndedQuestion>(QuestionType.OpenEnded)
            .HasValue<MultipleChoiceQuestion>(QuestionType.MultipleChoice)
            .HasValue<SingleChoiceQuestion>(QuestionType.SingleChoice)
            .HasValue<FillBlanksQuestion>(QuestionType.FillInBlanks);

        // Configure CorrectAnswers as a comma-separated string
        modelBuilder.Entity<Question>()
            .Property(q => q.CorrectAnswers)
            .HasConversion(
                v => string.Join(",", v), // Convert List<string> to a single CSV string
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() // Convert CSV back to List<string>
            );

        // Configure Options as a comma-separated string for MultipleChoiceQuestion
        modelBuilder.Entity<MultipleChoiceQuestion>()
            .Property(mcq => mcq.Options)
            .HasConversion(
                v => string.Join(",", v), // Serialize List<string> to CSV string
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() // Deserialize CSV back to List<string>
            );

        // Configure Options as a comma-separated string for SingleChoiceQuestion
        modelBuilder.Entity<SingleChoiceQuestion>()
            .Property(scq => scq.Options)
            .HasConversion(
                v => string.Join(",", v), // Serialize List<string> to CSV string
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList() // Deserialize CSV back to List<string>
            );

        // Self-referencing relationship for subquestions
        modelBuilder.Entity<Question>()
            .HasMany(q => q.SubQuestions)
            .WithOne(q => q.ParentQuestion)
            .HasForeignKey(q => q.ParentQuestionId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascading deletes

        modelBuilder.Entity<StudentTestSession>()
      .HasOne(sts => sts.TestAssignment)
      .WithMany()
      .HasForeignKey(sts => sts.TestAssignmentId)
      .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

        modelBuilder.Entity<TestQuestion>()
            .HasOne(tq => tq.Test)
            .WithMany(t => t.TestQuestions)
            .HasForeignKey(tq => tq.TestId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TestQuestion>()
            .HasOne(tq => tq.Question)
            .WithMany(q => q.TestQuestions)
            .HasForeignKey(tq => tq.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TestQuestion>()
            .HasIndex(tq => new { tq.TestId, tq.QuestionId })
            .IsUnique();
    }

}
