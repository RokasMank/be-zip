using Azure.Core;
using math4ktu_be.Data;
using math4ktu_be.Data.Models;
using math4ktu_be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace math4ktu_be.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("[controller]")]
public class AdminController : ControllerBase
{
    private readonly DatabaseContext _db;
    private readonly IAuthService _authService;

    public AdminController(DatabaseContext db, IAuthService authService)
    {
        _db = db;
        _authService = authService;
    }
    [HttpPost("create")]
    public async Task<IActionResult> CreateAdmin([FromBody] AdminCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (_db.Administrators.Any(a => a.Username == request.Username))
        {
            return Conflict(new { Message = "Username already exists." });
        }

        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            return NotFound(new { Message = "Role not found." });
        }

        var hashedPassword = PasswordHasher.HashPassword(request.Password);

        var admin = new Administrator
        {
            Username = request.Username,
            Password = hashedPassword,
            RoleId = adminRole.Id,
            Role = adminRole
        };

        _db.Administrators.Add(admin);
        await _db.SaveChangesAsync();

        return Ok(admin);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { Message = "Username and password are required." });
        }

        try
        {
            var token = await _authService.Authenticate(request.Username, request.Password);
            return Ok(token);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    [HttpGet("test")]
    [AllowAnonymous]
    public IActionResult Test()
    {
        return Ok(new { Message = "CORS test successful" });
    }

    [HttpGet("liveness")]
    [AllowAnonymous]
    public async Task<IActionResult> Liveness()
    {
        if (_db.Administrators.Any(a => a.Username == "AdminOne"))
        {
            return Conflict(new { Message = "Username already exists." });
        }

        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null)
        {
            return NotFound(new { Message = "Role not found." });
        }

        var hashedPassword = PasswordHasher.HashPassword("tadmin777");

        var admin = new Administrator
        {
            Username = "AdminOne",
            Password = hashedPassword,
            RoleId = adminRole.Id,
            Role = adminRole
        };

        _db.Administrators.Add(admin);
        await _db.SaveChangesAsync();

        return Ok();
    }

    //[HttpGet("{id}")]
    //public IActionResult GetAdminById(int id)
    //{
    //    var admin = _db.Admins.Find(id);
    //    if (admin == null)
    //    {
    //        return NotFound(new { Message = "Admin not found." });
    //    }

    //    return Ok(admin);
    //}
    public class AdminCreateRequest
    {
        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

    }
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
