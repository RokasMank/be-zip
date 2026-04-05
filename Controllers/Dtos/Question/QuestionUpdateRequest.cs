namespace math4ktu_be.Controllers.Dtos.Question;

public class QuestionUpdateRequest
{
    public string Text { get; set; }
    public List<string> CorrectAnswers { get; set; }
    public List<string> Options { get; set; }
}