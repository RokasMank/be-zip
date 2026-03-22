using CsvHelper.Configuration;
using CsvHelper;
using math4ktu_be.Data;
using math4ktu_be.Data.Models;
using math4ktu_be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using math4ktu_be.Controllers.Dtos.Student;

namespace math4ktu_be.Controllers;

[ApiController]
//[Authorize(Roles = "Admin")]
[Route("[controller]")]
public class StudentController: ControllerBase
{
    private readonly DatabaseContext _db;

    public StudentController(DatabaseContext db, IAuthService authService)
    {
        _db = db;
    }
    [HttpPost("create")]
    public async Task<IActionResult> CreateStudent([FromBody] StudentCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingStudent = await _db.Students.AnyAsync(s => s.Code == request.Code);
        if (existingStudent)
        {
            return Conflict("Unable to create student");
        }
        var student = new Student
        {
            Code = request.Code,
            Gender = request.Gender,
            StudentClass = request.Class,
            CreatedAt = DateTimeOffset.Now,
            School = request.School,
        };

        _db.Students.Add(student);
        await _db.SaveChangesAsync();

        return Ok(student);
    }
    [AllowAnonymous]
    [HttpPost("login/{code}")]
    public async Task<IActionResult> Login([FromRoute] string code)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var student = await _db.Students.FirstOrDefaultAsync(s => s.Code == code);

        if (student == null)
        {
            return NotFound();
        }
        return Ok(student);
    }
    [HttpPost("get/{id}")]
    public async Task<IActionResult> GetStudent([FromRoute] int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

       var student = await _db.Students.FindAsync(id);

        return Ok(student);
    }
    [HttpPost("getSessions/{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStudentTestSessions([FromRoute] int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var sessions = await _db.StudentTestSessions.Where(s => s.StudentId == id && s.SessionStatus == StudentSessionStatus.Published).Include(s => s.Test)
            .ToListAsync();

        return Ok(sessions);
    }
    [HttpPut("edit")]
    public async Task<IActionResult> EditStudent([FromBody] StudentEditRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate the request contains an ID
        if (!request.Id.HasValue)
        {
            return BadRequest(new { Message = "Student ID is required." });
        }

        // Find the student by ID
        var student = await _db.Students.FindAsync(request.Id.Value);
        if (student == null)
        {
            return NotFound(new { Message = "Student not found." });
        }

        // Check if at least one field is being updated
        if (string.IsNullOrWhiteSpace(request.Code) && request.Class == null && string.IsNullOrWhiteSpace(request.Gender))
        {
            return BadRequest(new { Message = "No valid changes provided." });
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            student.Code = request.Code;
        }

        if (!string.IsNullOrWhiteSpace(request.Class))
        {
            student.StudentClass = request.Class!;
        }

        if (!string.IsNullOrWhiteSpace(request.Gender))
        {
            student.Gender = request.Gender;
        }

        // Save changes to the database
        await _db.SaveChangesAsync();

        return Ok(student);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetStudents(
    [FromQuery] string? code,
    [FromQuery] string? gender,
    [FromQuery(Name = "class")] string? studentClass,
    [FromQuery] string? school,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return BadRequest(new { Message = "Page and pageSize must be greater than 0." });
        }

        var query = _db.Students.AsQueryable();

        if (!string.IsNullOrWhiteSpace(code))
        {
            // partial match (case-insensitive)
            query = query.Where(s => EF.Functions.Like(s.Code, $"%{code}%"));
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            query = query.Where(s => s.Gender == gender);
        }

        if (!string.IsNullOrWhiteSpace(studentClass))
        {
            query = query.Where(s => EF.Functions.Like(s.StudentClass, $"%{studentClass}%"));
        }

        if (!string.IsNullOrWhiteSpace(school))
        {
            query = query.Where(s => EF.Functions.Like(s.School, $"%{school}%"));
        }

        var total = await query.CountAsync();
        var students = await query
            .OrderBy(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Students = students
        });
    }


    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadStudents([FromForm] FileUploadRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Message = "No file uploaded." });
        }

        var studentsToAdd = new List<Student>();

        using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        }))
        {
            try
            {
                var records = csv.GetRecords<StudentCsvRow>().ToList();

                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Code)) continue;

                    // Skip existing students by code
                    var exists = await _db.Students.AnyAsync(s => s.Code == record.Code);

                    if (exists)
                    {
                        continue;
                    }

                    var student = new Student
                    {
                        Code = record.Code,
                        Gender = record.Gender,
                        StudentClass = record.StudentClass,
                        School = record.School,
                        CreatedAt = DateTimeOffset.Now
                    };

                    studentsToAdd.Add(student);
                }

                if (studentsToAdd.Any())
                {
                    _db.Students.AddRange(studentsToAdd);
                    await _db.SaveChangesAsync();
                }

                return Ok(new
                {
                    Added = studentsToAdd.Count,
                    Skipped = records.Count - studentsToAdd.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Failed to parse CSV file", Details = ex.Message });
            }
        }
    }

    public class StudentEditRequest
    {
        public int? Id { get; set; } // Include ID in the request body
        public string? Code { get; set; }
        public string? Class { get; set; }
        public string? Gender { get; set; }
        public string School { get; set; }
    }

    public class StudentCreateRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Class { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string School { get; set; } 
    }
}
