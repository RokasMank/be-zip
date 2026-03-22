namespace math4ktu_be.Data.Models;

using System.ComponentModel.DataAnnotations;

public class Student
{
    public int Id { get; set; }
    public required string Code {  get; set; }
    [Range(1, 4)]
    public required string StudentClass { get; set; }
    public string Gender { get; set; }
    public string School { get; set; }
    public DateTimeOffset CreatedAt = new DateTimeOffset();
}
