namespace math4ktu_be.Data.Models;

public class MultipleChoiceQuestion : Question
{
    public List<string> Options { get; set; } = new();
}
