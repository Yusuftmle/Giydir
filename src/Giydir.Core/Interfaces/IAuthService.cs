using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, string? name = null);
    Task<AuthResult> LoginAsync(string email, string password);
    Task LogoutAsync();
    Task<User?> GetCurrentUserAsync();
    int? GetCurrentUserId();
    string? GetToken();
}

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public string? Token { get; set; }

    public static AuthResult Ok(User user, string? token = null) => new() { Success = true, User = user, Token = token };
    public static AuthResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

