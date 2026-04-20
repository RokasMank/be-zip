using math4ktu_be.Data;
using math4ktu_be.Data.Enums;
using math4ktu_be.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace math4ktu_be.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles ="Admin")]
public class TestController : ControllerBase
{
    private readonly DatabaseContext _db;

    public TestController(DatabaseContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreateTest([FromBody] TestCreateRequest request)
    {
        var test = new Test
        {
            Title = request.Title,
            Description = request.Description,
        };

        _db.Tests.Add(test);
        await _db.SaveChangesAsync();

        return Ok(test);
    }
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTest([FromRoute] int id)
    {
        var test = await _db.Tests
            .Include(t => t.TestQuestions)
                .ThenInclude(tq => tq.Question)
                    .ThenInclude(q => q.SubQuestions)
            .SingleOrDefaultAsync(t => t.Id == id);

        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        var pointsByQuestionId = test.TestQuestions
            .GroupBy(tq => tq.QuestionId)
            .ToDictionary(g => g.Key, g => g.First().Points);

        var rootQuestions = test.TestQuestions
            .Select(tq => tq.Question)
            .Where(q => q.ParentQuestionId == null)
            .DistinctBy(q => q.Id)
            .ToList();

        // Map the Test entity to TestDto, including recursive subquestions
        var testDto = new TestDto
        {
            Id = test.Id,
            Title = test.Title,
            Description = test.Description,
            Published = test.Published,
            Questions = MapQuestionsWithSubQuestions(rootQuestions, pointsByQuestionId)
        };

        return Ok(testDto);
    }
    // Could be reused probably
    private List<QuestionDto> MapQuestionsWithSubQuestions(IEnumerable<Question> questions, Dictionary<int, double> pointsByQuestionId)
    {
        return questions.Select(q => new QuestionDto
        {
            Id = q.Id,
            Text = q.Text,
            TextWithBlanks = q is FillBlanksQuestion mc ? mc.TextWithBlanks : string.Empty,
            Points = pointsByQuestionId.GetValueOrDefault(q.Id, 0),
            ImageUrl = q.ImageUrl,
            QuestionType = q.QuestionType,
            Options = q is MultipleChoiceQuestion mcq ? mcq.Options :
                     q is SingleChoiceQuestion scq ? scq.Options : new List<string>(),
            MaxCharsAllowed = q is OpenEndedQuestion oeq ? oeq.MaxCharsAllowed : null, // Include MaxCharsAllowed for OpenEnded
            CorrectAnswers = q.CorrectAnswers,
            SubQuestions = q.SubQuestions != null && q.SubQuestions.Any()
                ? MapQuestionsWithSubQuestions(q.SubQuestions, pointsByQuestionId) // Recursively map subquestions
                : new List<QuestionDto>()
        }).ToList();
    }

    [HttpPost("{id}/publish")]
    public async Task<IActionResult> PublishTest([FromRoute] int id)
    {
        var test = await _db.Tests.SingleOrDefaultAsync(t => t.Id == id);

        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        if (test.Published)
        {
            return BadRequest(new { Message = "Test is already published." });
        }

        test.Published = true;
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Test published successfully." });
    }

    [HttpGet]
    public async Task<IActionResult> GetAllTests()
    {
        var tests = await _db.Tests
            .Select(t => new { t.Id, t.Title, t.Description }) // Minimize response size (Add published)
            .ToListAsync();

        return Ok(tests);
    }
    [HttpGet("published")]
    public async Task<IActionResult> GetPublishedTests()
    {
        var tests = await _db.Tests
            .Where(t => t.Published) // Filter only published tests
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                Published = t.Published
            })
            .ToListAsync();

        return Ok(tests);
    }


    public class TestCreateRequest
    {
        public string Title { get; set; } = string.Empty; // Test name
        public string? Description { get; set; } // Optional description
        //public TimeSpan? DefaultTimeLimit { get; set; } // Optional time limit
       // public bool IsDraft { get; set; } = true; // Defaults to true
    }

    public class TestDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool Published { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }

    public class QuestionDto
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string TextWithBlanks { get; set; } = string.Empty;

        public double Points { get; set; }
        public string ImageUrl { get; set; }
        public QuestionType QuestionType { get; set; }
        public List<string> Options { get; set; } = new();
        public List<string> CorrectAnswers { get; set; } = new();
        public int? MaxCharsAllowed { get; set; }
        public List<QuestionDto> SubQuestions { get; set; } = new(); // Nested subquestions
    }
}
