using math4ktu_be.Controllers.Dtos.Question;
using math4ktu_be.Data;
using math4ktu_be.Data.Enums;
using math4ktu_be.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace math4ktu_be.Controllers;

[ApiController]
[Route("[controller]")]
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
        var test = await _db.Tests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == testId);


        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        var question = BuildQuestion(request); // Build the question from the request
        test.Questions.Add(question);

        await _db.SaveChangesAsync();

        return Ok(question);
    }

    [HttpDelete("{testId}/questions/{questionId}")]
    public async Task<IActionResult> RemoveQuestionFromTest([FromRoute] int testId, [FromRoute] int questionId)
    {
        var test = await _db.Tests.Include(t => t.Questions).SingleOrDefaultAsync(t => t.Id == testId);

        if (test == null)
        {
            return NotFound(new { Message = "Test not found." });
        }

        if (test.Published)
        {
            return BadRequest(new { Message = "Cannot remove questions from a published test." });
        }

        var question = test.Questions.SingleOrDefault(q => q.Id == questionId);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found." });
        }

        test.Questions.Remove(question);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Question removed successfully." });
    }

    [HttpPut("{id}")]
    public IActionResult UpdateQuestion(int id, [FromBody] QuestionUpdateRequest request)
    {
        var question = _db.Questions.FirstOrDefault(q => q.Id == id);

        if (question == null)
        {
            return NotFound(new { Message = "Question not found" });
        }

        // Update the question's properties
        question.Text = request.Text ?? question.Text;
        question.Points = request.Points ?? question.Points;
        question.CorrectAnswers = request.CorrectAnswers ?? question.CorrectAnswers;

        _db.SaveChanges();

        return Ok(question);
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
        question.Points = request.Points;
        question.ImageUrl = request.ImageUrl;
        question.AchievementArea = request.AchievementArea;
        question.CognitiveArea = request.CognitiveArea;
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
}
