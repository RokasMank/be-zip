using System.ComponentModel.DataAnnotations;

namespace math4ktu_be.Controllers.Dtos.Student;

public class FileUploadRequest
{
    [Required]
    public IFormFile File { get; set; }
}