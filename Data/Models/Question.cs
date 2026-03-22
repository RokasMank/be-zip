using math4ktu_be.Data.Enums;
using System.Text.Json.Serialization;

namespace math4ktu_be.Data.Models;

public abstract class Question
{
    public int Id { get; set; }
    public string? ExternalId { get; set; }
    public string Text { get; set; } = string.Empty!;
    public string? ImageUrl { get; set; }
    public QuestionType QuestionType { get; set; }
    public List<string> CorrectAnswers { get; set; } = [];
    public int Points { get; set; }
    public int? ParentQuestionId { get; set; }
    [JsonIgnore] // Prevents cyclic reference

    public Question? ParentQuestion { get; set; }
    [JsonIgnore] // Prevents cyclic reference

    public List<Question> SubQuestions { get; set; } = [];

    public QuestionCategoryClass QuestionCategoryClass { get; set; }
    public ContentType ContentType { get; set; }
    public AchievementArea AchievementArea { get; set; }
    public CognitiveArea CognitiveArea { get; set; }
}