namespace math4ktu_be.Data.Models;

public class OpenEndedQuestion : Question
{
    public bool AllowsMultipleAnswers { get; set; } = false;
    public int MaxCharsAllowed { get; set; } = 5000;
}
