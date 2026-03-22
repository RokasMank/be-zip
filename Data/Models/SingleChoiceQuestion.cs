namespace math4ktu_be.Data.Models;

public class SingleChoiceQuestion : Question
{
    public List<string> Options { get; set; } = new();
    public bool AllowsMultipleAnswers = false;
}