using math4ktu_be.Data.Models;
using math4ktu_be.Services;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using math4ktu_be.Data;
using Microsoft.EntityFrameworkCore;

public class AuthService : IAuthService
{
    private readonly DatabaseContext _db;
    private readonly IConfiguration _configuration;

    public AuthService(DatabaseContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<string> Authenticate(string username, string password)
    {
        // Fetch the user (Admin in this case) from the database
        var admin = await _db.Administrators.Include(r => r.Role).FirstOrDefaultAsync
            (a => a.Username == username);

        if (admin == null || !PasswordHasher.VerifyPassword(password, admin.Password))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var token = GenerateJwtToken(admin);
        // Generate JWT token
        return token;
    }

    private string GenerateJwtToken(Administrator admin)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claims to include in the JWT token
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()), // User ID
            new Claim(JwtRegisteredClaimNames.Name, admin.Username),    // Username
            new Claim(ClaimTypes.Role, admin.Role.Name),                // Role
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique identifier for the token
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1), // Token expiration time
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
