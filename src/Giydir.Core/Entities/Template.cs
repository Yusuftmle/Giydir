namespace Giydir.Core.Entities;

public class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    
    // Kıyafet özellikleri
    public string Style { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    
    // YENİ: Sahne özellikleri (model'in default'larını override edecek)
    public string? Background { get; set; }
    public string? Lighting { get; set; }
    public string? Pose { get; set; }
    public string? CameraAngle { get; set; }
    public string? Mood { get; set; }

    public bool RequiresModel { get; set; } = false;
    
    public string? AdditionalAttributes { get; set; }
    public string PromptTemplate { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


