namespace Giydir.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int Credits { get; set; } = 10;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Profil alanları
    public string? Name { get; set; }
    public string? Title { get; set; }
    public string? BoutiqueName { get; set; }
    public string? Sector { get; set; }
    public string? WebsiteUrl { get; set; }

    public string Role { get; set; } = "User";

    public List<Project> Projects { get; set; } = new();
}

