namespace Giydir.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public List<GeneratedImage> Images { get; set; } = new();
}




