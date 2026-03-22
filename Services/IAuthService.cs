namespace math4ktu_be.Services;

public interface IAuthService
{
    Task<string> Authenticate(string username, string password);
}

