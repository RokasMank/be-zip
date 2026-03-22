using math4ktu_be.Data;
using math4ktu_be.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static math4ktu_be.Controllers.TestController;

namespace math4ktu_be.Controllers;

[ApiController]
[Route("[controller]")]
public class TestSessionController : ControllerBase
{
    private readonly DatabaseContext _db;

    public TestSessionController(DatabaseContext db)
    {
        _db = db;
    }

    // Get Test Session Details
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTestSession([FromRoute] int id)
    {
        var session = await _db.StudentTestSessions
            .Include(s => s.Test)
                .ThenInclude(t => t.Questions)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound(new { Message = "Test session not found." });
        }

        var sessionDetails = new
        {
            session.Id,
            Test = new
            {
                session.Test.Title,
                session.Test.Description,
                Questions = session.Test.Questions.Select(q => new
                {
                    q.Id,
                    q.Text,
                    q.Points,
                    q.QuestionType,
                    Options = q is MultipleChoiceQuestion mcq ? mcq.Options :
                             q is SingleChoiceQuestion scq ? scq.Options : [],
                    CorrectAnswers = session.SessionStatus == StudentSessionStatus.Finished ? q.CorrectAnswers : null
                }).ToList()
            },
            session.SessionStatus
        };

        return Ok(sessionDetails);
    }

    [HttpPut("start/{id}")]
    public async Task<IActionResult> StartSession([FromRoute] int id)
    {
        var session = await _db.StudentTestSessions
            .Include(s => s.Test)
                .ThenInclude(t => t.Questions)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound(new { Message = "Test session not found." });
        }

        session.StartTime = DateTimeOffset.Now;

        await _db.SaveChangesAsync();
        return Ok(session);
    }

    [HttpPut("submit-answer")]
    public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var studentTestSession = await _db.StudentTestSessions
            .Include(s => s.Answers)
            .SingleOrDefaultAsync(s => s.Id == request.StudentTestSessionId);

        if (studentTestSession == null)
        {
            return NotFound(new { Message = "Test session not found." });
        }

        if (studentTestSession.SessionStatus == StudentSessionStatus.Finished)
        {
            return BadRequest(new { Message = "Test is already finished." });
        }

        var allAnswers = FlattenAnswers(request.Answers);

        foreach (var answer in allAnswers)
        {
            var question = await _db.Questions.SingleOrDefaultAsync(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            var existingAnswer = studentTestSession.Answers
                .SingleOrDefault(a => a.QuestionId == answer.QuestionId);

            // Handle scenarios with no correct answers or only textual questions
            double pointsEarned = 0;

            if (question.CorrectAnswers.Any()) // Calculate points only if correct answers exist
            {
                var correctAnswers = new HashSet<string>(question.CorrectAnswers);
                var providedAnswers = new HashSet<string>(answer.ProvidedAnswers);

                int totalCorrectAnswers = correctAnswers.Count;
                int correctlyAnswered = providedAnswers.Count(pa => correctAnswers.Contains(pa));
                int incorrectlyAnswered = providedAnswers.Count(pa => !correctAnswers.Contains(pa));

                // Partial credit formula
                double credit = Math.Max(0, (double)correctlyAnswered / totalCorrectAnswers - (double)incorrectlyAnswered / totalCorrectAnswers);
                pointsEarned = Math.Round(credit * question.Points, 2);
            }

            // Update or add answer
            if (existingAnswer != null)
            {
                existingAnswer.ProvidedAnswers = answer.ProvidedAnswers;
                existingAnswer.PointsEarned = pointsEarned;
                existingAnswer.AnsweredAt = DateTime.UtcNow;
            }
            else
            {
                _db.StudentAnswers.Add(new StudentAnswer
                {
                    StudentTestSessionId = request.StudentTestSessionId,
                    QuestionId = answer.QuestionId,
                    ProvidedAnswers = answer.ProvidedAnswers,
                    PointsEarned = pointsEarned,
                    AnsweredAt = DateTime.UtcNow,
                });
            }
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Answers submitted successfully." });
    }


    private List<StudentAnswerRequest> FlattenAnswers(List<StudentAnswerRequest> answers)
    {
        var flatAnswers = new List<StudentAnswerRequest>();

        foreach (var answer in answers)
        {
            flatAnswers.Add(answer);

            // Recursively process subquestions
            if (answer.SubAnswers != null && answer.SubAnswers.Any())
            {
                flatAnswers.AddRange(FlattenAnswers(answer.SubAnswers));
            }
        }

        return flatAnswers;
    }

    public class StudentAnswerRequest
    {
        public int QuestionId { get; set; } // The ID of the question being answered
        public List<string> ProvidedAnswers { get; set; } = new(); // Answers provided by the student
        public List<StudentAnswerRequest>? SubAnswers { get; set; } = new(); // Answers for subquestions
    }

    public class SubmitAnswerRequest
    {
        public int StudentTestSessionId { get; set; } // The ID of the test session
        public List<StudentAnswerRequest> Answers { get; set; } = new(); // List of all answers
    }


    [HttpPost("{id}/complete")]
    public async Task<IActionResult> CompleteTestSession([FromRoute] int id)
    {
        var session = await _db.StudentTestSessions
            .Include(s => s.Answers)
            .Include(s => s.Test)
                .ThenInclude(t => t.Questions)
            .SingleOrDefaultAsync(s => s.Id == id);

        if (session == null)
        {
            return NotFound(new { Message = "Test session not found." });
        }

        if (session.SessionStatus == StudentSessionStatus.Finished)
        {
            return BadRequest(new { Message = "Test session is already completed." });
        }

        var totalScore = session.Answers.Sum(answer =>
        {
            var question = session.Test.Questions.SingleOrDefault(q => q.Id == answer.QuestionId);
            return answer.IsCorrect ? question?.Points ?? 0 : 0;
        });

        session.Score = totalScore;
        session.SessionStatus = StudentSessionStatus.Finished;
        session.EndTime = DateTimeOffset.Now;

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Test session completed.", TotalScore = totalScore });
    }

    [HttpGet("{sessionId}/results")]
    public async Task<IActionResult> GetTestResults([FromRoute] int sessionId)
    {
        var session = await _db.StudentTestSessions
            .Include(s => s.Test)
                .ThenInclude(t => t.Questions)
                    .ThenInclude(q => q.SubQuestions)
            .Include(s => s.Answers)
            .SingleOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
        {
            return NotFound(new { Message = "Test session not found." });
        }

        var results = GenerateResults(session.Test.Questions, session.Answers);

        return Ok(new
        {
            Title = session.Test.Title,
            Description = session.Test.Description,
            TotalScore = session.Test.Questions.Sum(q => q.Points),
            ScoreEarned = session.Answers.Sum(a => a.PointsEarned),
            Results = results
        });
    }
    private List<QuestionResult> GenerateResults(IEnumerable<Question> questions, ICollection<StudentAnswer> answers)
    {
        var results = new List<QuestionResult>();

        foreach (var question in questions)
        {
            // Find the corresponding answer for the question
            var answer = answers.SingleOrDefault(a => a.QuestionId == question.Id);

            // Recursively handle subquestions
            var subResults = question.SubQuestions.Any()
                ? GenerateResults(question.SubQuestions, answers)
                : null;

            // Construct the result object
            var result = new QuestionResult
            {
                Question = new QuestionDto
                {
                    Id = question.Id,
                    Text = question.Text,
                    Points = question.Points,
                    QuestionType = question.QuestionType,
                    Options = question switch
                    {
                        MultipleChoiceQuestion mcq => mcq.Options,
                        SingleChoiceQuestion scq => scq.Options,
                        _ => new List<string>()
                    },
                    CorrectAnswers = question.CorrectAnswers
                },
                ProvidedAnswers = answer?.ProvidedAnswers ?? new List<string>(),
                PointsEarned = (answer?.PointsEarned ?? 0) + (subResults?.Sum(sr => sr.PointsEarned) ?? 0),
                IsCorrect = answer?.PointsEarned == question.Points,
                SubResults = subResults
            };

            results.Add(result);
        }

        return results;
    }
    public class QuestionResult
    {
        public QuestionDto Question { get; set; } = null!;
        public List<string> ProvidedAnswers { get; set; } = new();
        public double PointsEarned { get; set; }
        public bool IsCorrect { get; set; }
        public List<QuestionResult>? SubResults { get; set; }
    }

}
