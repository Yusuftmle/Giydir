namespace Giydir.Core.DTOs;

public class RegisterDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Name { get; set; }
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public int Credits { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? BoutiqueName { get; set; }
    public string? Sector { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? Role { get; set; }
}

public class UpdateProfileDto
{
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? BoutiqueName { get; set; }
    public string? Sector { get; set; }
    public string? WebsiteUrl { get; set; }
}

public class AuthResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Token { get; set; }
    public UserDto? User { get; set; }
}

