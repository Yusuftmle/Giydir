namespace Giydir.Core.Entities;

public class Pose
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;           // "Ayakta", "Rahat", "Editorial"
    public string ImagePath { get; set; } = string.Empty;       // "/uploads/poses/standing.jpg"
    public string PromptKeyword { get; set; } = string.Empty;   // "standing pose", "casual pose"
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
