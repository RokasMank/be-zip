using CsvHelper;
using CsvHelper.Configuration;
using math4ktu_be.Data;
using math4ktu_be.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("[controller]")]
public class TestAssignmentController : ControllerBase
{
    private readonly DatabaseContext _db;

    public TestAssignmentController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpPost("{assignmentId}/add-student")]
    public async Task<IActionResult> AddStudentToAssignment(
       [FromRoute] int assignmentId,
       [FromBody] AddStudentRequest request)
    {
        // Check if the assignment exists
        var assignment = await _db.TestAssignments
            .Include(a => a.StudentsSessions) // Include students sessions in the assignment
            .SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Test assignment not found." });
        }

        // Check if the student exists
        var student = await _db.Students.SingleOrDefaultAsync(s => s.Code == request.StudentCode);

        if (student == null)
        {
            return NotFound(new { Message = "Student not found." });
        }

        // Check if the student is already added
        if (assignment.Students.Any(s => s.Id == student.Id))
        {
            return BadRequest(new { Message = "Student is already assigned to this test." });
        }

        var studentSession = new StudentTestSession
        {
            Student = student,
            StudentId = student.Id,
            Test = assignment.Test,
            TestId = assignment.TestId,
            TestAssignment = assignment,
        };

        // Add student to the assignment
        assignment.StudentsSessions.Add(studentSession);

        await _db.SaveChangesAsync();

        return Ok(student);
    }

    [HttpDelete("{assignmentId}/remove-student/{code}")]
    public async Task<IActionResult> RemoveStudentFromAssignment(
    [FromRoute] int assignmentId,
    [FromRoute] string code)
    {
        // Fetch the assignment including its students
        var assignment = await _db.TestAssignments
            .Include(a => a.Students)
            .SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Test assignment not found." });
        }

        // Check if the student is part of the assignment
        var student = assignment.Students.SingleOrDefault(s => s.Code == code);
        if (student == null)
        {
            return NotFound(new { Message = "Student not found in this assignment." });
        }

        // Remove the student from the assignment
        assignment.Students.Remove(student);

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Student successfully removed from the assignment." });
    }

    // Create a new test assignment
    [HttpPost("create")]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentRequest request)
    {
        var test = await _db.Tests.SingleOrDefaultAsync(t => t.Id == request.TestId && t.Published);
        if (test == null)
        {
            return BadRequest(new { Message = "The selected test is not published or does not exist." });
        }

        var assignment = new TestAssignment
        {
            TestId = request.TestId,
            Class = request.Class,
            Title = request.Title,
            Description = request.Description,
            TestAssignmentStatus = TestAssignmentStatus.Draft,
            Students = new List<Student>()
        };

        _db.TestAssignments.Add(assignment);
        await _db.SaveChangesAsync();

        return Ok(new { AssignmentId = assignment.Id, Message = "Assignment created successfully." });
    }

    // Get assignment details
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAssignmentDetails([FromRoute] int id)
    {
        var assignment = await _db.TestAssignments
            .Include(a => a.StudentsSessions)
            .ThenInclude(s => s.Student)
            .Include(a => a.Test)
            .ThenInclude(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
            .SingleOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        var pointsByQuestionId = assignment.Test.TestQuestions
            .GroupBy(tq => tq.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().Points);

        var rootQuestions = assignment.Test.TestQuestions
            .Select(tq => tq.Question)
            .Where(q => q.ParentQuestionId == null)
            .DistinctBy(q => q.Id)
            .ToList();

        return Ok(new
        {
            assignment.Id,
            assignment.TestId,
            assignment.Title,
            assignment.Description,
            assignment.Class,
            assignment.TestAssignmentStatus,
            Test = new
            {
                assignment.Test.Title,
                assignment.Test.Description,
                Questions = rootQuestions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    Points = pointsByQuestionId.GetValueOrDefault(q.Id, 0),
                    q.QuestionType,
                    Options = q is MultipleChoiceQuestion mcq ? mcq.Options :
                             q is SingleChoiceQuestion scq ? scq.Options : new List<string>(),
                    CorrectAnswers = q.CorrectAnswers
                }).ToList()
            },
            StudentSessions = assignment.StudentsSessions.Select(s => new
            {
                s.Student.Code,
                s.Student.StudentClass,
                s.Student.Gender,
                s.SessionStatus,
            }).ToList()
        });
    }

    // Get all assignments
    [HttpGet]
    public async Task<IActionResult> GetAllAssignments()
    {
        var assignments = await _db.TestAssignments
            .Include(a => a.Test)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Description,
                a.Class,
                a.TestAssignmentStatus,
                TestTitle = a.Test.Title
            })
            .ToListAsync();

        return Ok(assignments);
    }

    // Publish assignment
    [HttpPost("{assignmentId}/publish")]
    public async Task<IActionResult> PublishAssignment([FromRoute] int assignmentId)
    {
        var assignment = await _db.TestAssignments.Include(a => a.StudentsSessions).SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        if (assignment.TestAssignmentStatus != TestAssignmentStatus.Draft)
        {
            return BadRequest(new { Message = "Only draft assignments can be published." });
        }

        assignment.TestAssignmentStatus = TestAssignmentStatus.Published;
        assignment.PublishDate = DateTime.UtcNow;

        foreach (var session in assignment.StudentsSessions)
        {
            if (session.SessionStatus == StudentSessionStatus.Draft)
            {
                session.SessionStatus = StudentSessionStatus.Published;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Assignment published successfully." });
    }

    // Unpublish assignment
    [HttpPost("{assignmentId}/unpublish")]
    public async Task<IActionResult> UnpublishAssignment([FromRoute] int assignmentId)
    {
        var assignment = await _db.TestAssignments.Include(a => a.StudentsSessions).SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        if (assignment.TestAssignmentStatus != TestAssignmentStatus.Published)
        {
            return BadRequest(new { Message = "Only published assignments can be unpublished." });
        }

        assignment.TestAssignmentStatus = TestAssignmentStatus.Draft;
        assignment.PublishDate = null;

        foreach (var session in assignment.StudentsSessions)
        {
            if (session.SessionStatus == StudentSessionStatus.Published)
            {
                session.SessionStatus = StudentSessionStatus.Draft;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Assignment unpublished successfully." });
    }

    // Finish assignment
    [HttpPost("{assignmentId}/finish")]
    public async Task<IActionResult> FinishTestAssignment([FromRoute] int assignmentId)
    {
        var assignment = await _db.TestAssignments.Include(a => a.StudentsSessions).SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        if (assignment.TestAssignmentStatus == TestAssignmentStatus.Finished)
        {
            return BadRequest(new { Message = "Assignment is already finished." });
        }

        assignment.TestAssignmentStatus = TestAssignmentStatus.Finished;

        foreach (var session in assignment.StudentsSessions)
        {
            if (session.SessionStatus != StudentSessionStatus.Finished)
            {
                session.SessionStatus = StudentSessionStatus.Finished;
                //session.EndTime = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Assignment marked as finished successfully." });
    }

    // Update assignment
    [HttpPut("update/{assignmentId}")]
    public async Task<IActionResult> UpdateAssignment([FromRoute] int assignmentId, [FromBody] UpdateAssignmentRequest request)
    {
        var assignment = await _db.TestAssignments.SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        if (assignment.TestAssignmentStatus != TestAssignmentStatus.Draft)
        {
            return BadRequest(new { Message = "Only draft assignments can be updated." });
        }

        assignment.Title = request.Title ?? assignment.Title;
        assignment.Description = request.Description ?? assignment.Description;
        assignment.TestId = request.TestId > 0 ? request.TestId : assignment.TestId;
        assignment.Class = request.Class > 0 ? request.Class : assignment.Class;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Assignment updated successfully." });
    }

    // Delete assignment
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAssignment([FromRoute] int id)
    {
        var assignment = await _db.TestAssignments.Include(a => a.StudentsSessions).SingleOrDefaultAsync(a => a.Id == id);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        if (assignment.TestAssignmentStatus != TestAssignmentStatus.Draft)
        {
            return BadRequest(new { Message = "Only draft assignments can be deleted." });
        }

        _db.TestAssignments.Remove(assignment);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Assignment deleted successfully." });
    }

    [HttpGet("{assignmentId}/download-results")]
    public async Task<IActionResult> DownloadResults([FromRoute] int assignmentId)
    {
        var assignment = await _db.TestAssignments
            .Include(a => a.StudentsSessions)
                .ThenInclude(s => s.Student)
            .Include(a => a.StudentsSessions)
                .ThenInclude(s => s.Answers)
            .Include(a => a.Test)
                .ThenInclude(t => t.TestQuestions)
                    .ThenInclude(tq => tq.Question)
                        .ThenInclude(q => q.SubQuestions)
            .SingleOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null)
        {
            return NotFound(new { Message = "Assignment not found." });
        }

        var pointsByQuestionId = assignment.Test.TestQuestions
            .GroupBy(tq => tq.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().Points);

        var rootQuestions = assignment.Test.TestQuestions
            .Select(tq => tq.Question)
            .Where(q => q.ParentQuestionId == null)
            .DistinctBy(q => q.Id)
            .ToList();

        var headers = new List<string>();
        GenerateHeaders(rootQuestions, headers, "K");

        var flatQuestions = FlattenQuestions(rootQuestions);

        var results = assignment.StudentsSessions
            .Where(s => s.SessionStatus == StudentSessionStatus.Finished)
            .Select(s => new
            {
                School = s.Student.School,
                Class = s.Student.StudentClass,
                Code = s.Student.Code,
                Gender = s.Student.Gender,
                StartTime = s.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
                EndTime = s.EndTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Duration = (int)(s.EndTime - s.StartTime).TotalMinutes,
                QuestionScores = flatQuestions
                    .Select(q =>
                    {
                        var answer = s.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                        var points = pointsByQuestionId.GetValueOrDefault(q.Id, 0);
                        if (points == 0)
                        {
                            return "N/A";
                        }

                        return answer != null ? answer.PointsEarned.ToString() : "0";
                    })
            }).ToList();

        var memoryStream = new MemoryStream();
        using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";" // Set delimiter to semicolon
        }))
        {
            // Write headers
            csv.WriteField("Mokykla");
            csv.WriteField("Klasė");
            csv.WriteField("Kodas");
            csv.WriteField("Lytis");
            csv.WriteField("Pradžia");
            csv.WriteField("Pabaiga");
            csv.WriteField("Trukmė");
            foreach (var header in headers)
            {
                csv.WriteField(header);
            }
            csv.NextRecord();

            // Write rows
            foreach (var result in results)
            {
                csv.WriteField(result.School);
                csv.WriteField(result.Class);
                csv.WriteField(result.Code);
                csv.WriteField(result.Gender);
                csv.WriteField(result.StartTime);
                csv.WriteField(result.EndTime);
                csv.WriteField(result.Duration);
                foreach (var score in result.QuestionScores)
                {
                    csv.WriteField(score); // Write score or "N/A"
                }
                csv.NextRecord();
            }
        }

        // Reset the stream position to the beginning
        memoryStream.Position = 0;

        // Return the CSV file
        return File(memoryStream, "text/csv", $"assignment_{assignmentId}_results.csv");
    }

    private void GenerateHeaders(IEnumerable<Question> questions, List<string> headers, string prefix)
    {
        int index = 1;
        foreach (var question in questions)
        {
            var header = $"{prefix}{index}";
            headers.Add(header);
            if (question.SubQuestions.Any())
            {
                GenerateHeaders(question.SubQuestions, headers, $"{header}.");
            }
            index++;
        }
    }
    private List<Question> FlattenQuestions(IEnumerable<Question> questions)
    {
        var flatList = new List<Question>();
        foreach (var question in questions)
        {
            flatList.Add(question);
            if (question.SubQuestions.Any())
            {
                flatList.AddRange(FlattenQuestions(question.SubQuestions));
            }
        }
        return flatList;
    }

    public class CreateAssignmentRequest
    {
        public int TestId { get; set; }
        public int Class { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateAssignmentRequest
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int TestId { get; set; }
        public int Class { get; set; }
    }
    public class AddStudentRequest
    {
        public string StudentCode { get; set; } = string.Empty; // The student's unique code
    }
}
