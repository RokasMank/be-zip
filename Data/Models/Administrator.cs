namespace math4ktu_be.Data.Models;

public class Administrator
{
    public int Id { get; set; } 
    public string Username { get; set; } = string.Empty; 
    public string Password { get; set; } = string.Empty; //hashed
    public int RoleId { get; set; }
    public Role Role { get; set; } = new Role();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // When the admin was created
}