namespace math4ktu_be.Controllers.Dtos.Student;

public class StudentCsvRow
{
    public required string Code { get; set; }
    public required string StudentClass { get; set; }
    public string Gender { get; set; }
    public string School { get; set; }
}
