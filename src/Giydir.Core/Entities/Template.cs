namespace Giydir.Core.Entities;

public class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // upper_body, lower_body, dresses
    public string ThumbnailPath { get; set; } = string.Empty;
    
    // JSON verileri (prompt oluşturmak için)
    public string Style { get; set; } = string.Empty; // classic, modern, sporty, elegant
    public string Color { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty; // solid, striped, floral, etc.
    public string Material { get; set; } = string.Empty; // cotton, silk, denim, etc.
    public string? AdditionalAttributes { get; set; } // JSON string for extra attributes
    
    // Prompt template (JSON'dan oluşturulacak)
    public string PromptTemplate { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}


