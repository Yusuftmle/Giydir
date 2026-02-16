using System;

namespace Giydir.Core.Entities;

public class SavedPrompt
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public string? ResultImageUrl { get; set; }
    
    // AI Ayarları (JSON olarak saklanabilir veya ayrı alanlar)
    public string? SettingsJson { get; set; } // Aspect ratio, steps, temp vb.
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    
    public bool IsFavorite { get; set; }
    
    // Yayınlanan model ile ilişki (isteğe bağlı)
    public string? PublishedModelId { get; set; }
}
