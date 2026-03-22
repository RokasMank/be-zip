using math4ktu_be.Data.Enums;
using System.Text.Json.Serialization;

namespace math4ktu_be.Controllers.Dtos.Question;

public class QuestionCreateRequest
{
    public string Text { get; set; }
    public string? TextWithBlanks { get; set; }
    public string? ImageUrl { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionType QuestionType { get; set; }
    public int Points { get; set; }

    // Shared correct answers
    public List<string> CorrectAnswers { get; set; } = new();
    public int? MaxCharsAllowed { get; set; }

    // Specific for choice questions
    public List<string> Options { get; set; } = new();

    // Subquestions
    public List<QuestionCreateRequest>? SubQuestions { get; set; } = [];

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public QuestionCategoryClass QuestionCategoryClass { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ContentType ContentType { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CognitiveArea CognitiveArea { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AchievementArea AchievementArea { get; set; }
}