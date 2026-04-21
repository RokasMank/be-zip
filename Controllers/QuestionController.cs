using math4ktu_be.Controllers.Dtos.Question;
using math4ktu_be.Data;
using math4ktu_be.Data.Enums;
using math4ktu_be.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace math4ktu_be.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class QuestionController : ControllerBase
{

    private readonly DatabaseContext _db;

    public QuestionController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpPost]
    [Route("{testId}/questions")]
    public async Task<IActionResult> AddQuestionToTest(int testId, [FromBody] QuestionCreateRequest request)
    {
        var test = await _db.Tests
            .Include(t => t.TestQuestions)
            .FirstOrDefaultAsync(t => t.Id == testId);


        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        if (test.Published)
        {
            return BadRequest(new { Message = "Cannot modify questions in a published test." });
        }

        var question = BuildQuestion(request); // Create a bank question first
        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        var questionPointPairs = new List<(Question Question, int Points)>();
        CollectQuestionsWithPoints(question, request, questionPointPairs);

        foreach (var (createdQuestion, points) in questionPointPairs)
        {
            var alreadyLinked = test.TestQuestions.Any(tq => tq.QuestionId == createdQuestion.Id);
            if (!alreadyLinked)
            {
                _db.TestQuestions.Add(new TestQuestion
                {
                    TestId = testId,
                    QuestionId = createdQuestion.Id,
                    Points = points
                });
            }
        }

        await _db.SaveChangesAsync();

        return Ok(new { Message = "Question created and linked to test.", QuestionId = question.Id });
    }

    [HttpPost("bank")]
    public async Task<IActionResult> CreateBankQuestion([FromBody] QuestionCreateRequest request)
    {
        var question = BuildQuestion(request);
        _db.Questions.Add(question);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Question added to bank.", QuestionId = question.Id });
    }

    [HttpGet("bank")]
    public async Task<IActionResult> GetQuestionBank(
        [FromQuery] QuestionType? questionType,
        [FromQuery] QuestionCategoryClass? questionCategoryClass,
        [FromQuery] ContentType? contentType,
        [FromQuery] AchievementArea? achievementArea)
    {
        var query = _db.Questions
            .Where(q => q.ParentQuestionId == null)
            .Include(q => q.SubQuestions)
            .AsQueryable();

        if (questionType.HasValue)
        {
            query = query.Where(q => q.QuestionType == questionType.Value);
        }

        if (questionCategoryClass.HasValue)
        {
            query = query.Where(q => q.QuestionCategoryClass == questionCategoryClass.Value);
        }

        if (contentType.HasValue)
        {
            query = query.Where(q => q.ContentType == contentType.Value);
        }

        if (achievementArea.HasValue)
        {
            query = query.Where(q => q.AchievementArea == achievementArea.Value);
        }

        var questions = await query.ToListAsync();

        return Ok(questions.Select(MapQuestionToBankDto));
    }

    [HttpPost("{testId}/questions/from-bank")]
    public async Task<IActionResult> AddQuestionFromBank([FromRoute] int testId, [FromBody] AddQuestionFromBankRequest request)
    {
        var test = await _db.Tests
            .Include(t => t.TestQuestions)
            .SingleOrDefaultAsync(t => t.Id == testId);

        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        if (test.Published)
        {
            return BadRequest(new { Message = "Cannot modify questions in a published test." });
        }

        var rootQuestion = await _db.Questions.SingleOrDefaultAsync(q => q.Id == request.QuestionId);
        if (rootQuestion == null)
        {
            return NotFound(new { Message = "Bank question not found." });
        }

        var subtreeIds = await GetSubtreeQuestionIds(request.QuestionId);
        subtreeIds.Insert(0, request.QuestionId);

        foreach (var questionId in subtreeIds.Distinct())
        {
            var existing = test.TestQuestions.SingleOrDefault(tq => tq.QuestionId == questionId);
            if (existing != null)
            {
                existing.Points = ResolvePoints(request, questionId);
                continue;
            }

            _db.TestQuestions.Add(new TestQuestion
            {
                TestId = testId,
                QuestionId = questionId,
                Points = ResolvePoints(request, questionId)
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { Message = "Question linked from bank to test." });
    }

    [HttpDelete("{testId}/questions/{questionId}")]
    public async Task<IActionResult> RemoveQuestionFromTest([FromRoute] int testId, [FromRoute] int questionId)
    {
        var test = await _db.Tests
            .Include(t => t.TestQuestions)
            .SingleOrDefaultAsync(t => t.Id == testId);

        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        if (test.Published)
        {
            return BadRequest(new { Message = "Cannot remove questions from a published test." });
        }

        var subtreeIds = await GetSubtreeQuestionIds(questionId);
        subtreeIds.Insert(0, questionId);

        var linksToRemove = test.TestQuestions
            .Where(tq => subtreeIds.Contains(tq.QuestionId))
            .ToList();

        if (!linksToRemove.Any())
        {
            return NotFound(new { Message = "Question not found." });
        }

        _db.TestQuestions.RemoveRange(linksToRemove);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Question removed successfully." });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateQuestion(int id, [FromBody] QuestionUpdateRequest request)
    {
        var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found" });
        }

        // Update the question's properties
        question.Text = request.Text ?? question.Text;
        question.CorrectAnswers = request.CorrectAnswers ?? question.CorrectAnswers;

        await _db.SaveChangesAsync();

        return Ok(question);
    }

    private static int ResolvePoints(AddQuestionFromBankRequest request, int questionId)
    {
        var overrideEntry = request.PointOverrides?.FirstOrDefault(x => x.QuestionId == questionId);
        if (overrideEntry != null)
        {
            return overrideEntry.Points;
        }

        return request.DefaultPoints;
    }

    private async Task<List<int>> GetSubtreeQuestionIds(int rootQuestionId)
    {
        var children = await _db.Questions
            .Where(q => q.ParentQuestionId == rootQuestionId)
            .Select(q => q.Id)
            .ToListAsync();

        var all = new List<int>();
        foreach (var childId in children)
        {
            all.Add(childId);
            var nested = await GetSubtreeQuestionIds(childId);
            all.AddRange(nested);
        }

        return all;
    }

    private static QuestionBankDto MapQuestionToBankDto(Question question)
    {
        return new QuestionBankDto
        {
            Id = question.Id,
            Text = question.Text,
            ImageUrl = question.ImageUrl,
            QuestionType = question.QuestionType,
            QuestionCategoryClass = question.QuestionCategoryClass,
            ContentType = question.ContentType,
            AchievementArea = question.AchievementArea,
            Options = question switch
            {
                MultipleChoiceQuestion mcq => mcq.Options,
                SingleChoiceQuestion scq => scq.Options,
                _ => []
            },
            CorrectAnswers = question.CorrectAnswers,
            TextWithBlanks = question is FillBlanksQuestion fib ? fib.TextWithBlanks : string.Empty,
            MaxCharsAllowed = question is OpenEndedQuestion oeq ? oeq.MaxCharsAllowed : null,
            SubQuestions = question.SubQuestions.Select(MapQuestionToBankDto).ToList()
        };
    }

    private static void CollectQuestionsWithPoints(
        Question question,
        QuestionCreateRequest request,
        List<(Question Question, int Points)> accumulator)
    {
        accumulator.Add((question, request.Points));

        if (request.SubQuestions == null || !request.SubQuestions.Any())
        {
            return;
        }

        for (var i = 0; i < request.SubQuestions.Count && i < question.SubQuestions.Count; i++)
        {
            CollectQuestionsWithPoints(question.SubQuestions[i], request.SubQuestions[i], accumulator);
        }
    }

    private static Question BuildQuestion(QuestionCreateRequest request)
    {
        Question question = request.QuestionType switch
        {
            QuestionType.MultipleChoice => new MultipleChoiceQuestion
            {
                Options = request.Options ?? [],
                CorrectAnswers = request.CorrectAnswers ?? []
            },
            QuestionType.SingleChoice => new SingleChoiceQuestion
            {
                Options = request.Options ?? [],
                CorrectAnswers = request.CorrectAnswers.Count == 1 ? request.CorrectAnswers : throw new Exception("Wrong correct answers"),
                AllowsMultipleAnswers = false
            },
            QuestionType.OpenEnded => new OpenEndedQuestion
            {
                CorrectAnswers = request.CorrectAnswers ?? [],
                MaxCharsAllowed = request.MaxCharsAllowed ?? 5000,
                AllowsMultipleAnswers = true

            },
            QuestionType.FillInBlanks => new FillBlanksQuestion
            {
                TextWithBlanks = request.TextWithBlanks ?? throw new Exception("Question with blank spaces to fill could not be created because correct answer text with blanks was not received"),
                CorrectAnswers = request.CorrectAnswers
            },
            _ => throw new ArgumentException("Invalid question type")
        };

        question.Text = request.Text;
        question.ImageUrl = request.ImageUrl;
        question.AchievementArea = request.AchievementArea;
        question.ContentType = request.ContentType;
        question.QuestionCategoryClass = request.QuestionCategoryClass;

        if (request.SubQuestions != null)
        {
            foreach (var subQuestionRequest in request.SubQuestions)
            {
                var subQuestion = BuildQuestion(subQuestionRequest);
                subQuestion.ParentQuestion = question;
                question.SubQuestions.Add(subQuestion);
            }
        }

        return question;
    }

    public class AddQuestionFromBankRequest
    {
        public int QuestionId { get; set; }
        public int DefaultPoints { get; set; } = 1;
        public List<QuestionPointOverride>? PointOverrides { get; set; }
    }

    public class QuestionPointOverride
    {
        public int QuestionId { get; set; }
        public int Points { get; set; }
    }

    public class QuestionBankDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public QuestionType QuestionType { get; set; }
        public QuestionCategoryClass QuestionCategoryClass { get; set; }
        public ContentType ContentType { get; set; }
        public AchievementArea AchievementArea { get; set; }
        public List<string> Options { get; set; } = [];
        public List<string> CorrectAnswers { get; set; } = [];
        public string? TextWithBlanks { get; set; }
        public int? MaxCharsAllowed { get; set; }
        public List<QuestionBankDto> SubQuestions { get; set; } = [];
    }
}
