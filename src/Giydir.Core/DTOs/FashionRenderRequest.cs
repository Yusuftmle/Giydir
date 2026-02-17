namespace Giydir.Core.DTOs;

public class FashionRenderRequest
{
    public required string ProductCategory { get; set; }
    public required string Fit { get; set; }
    public required string Color { get; set; }
    public required string Vibe { get; set; } // Studio Minimal, Urban Cinematic, etc.
    public required string ModelId { get; set; }
    
    // Unified Engine: If SourceImage is present, it's a VTON/Img2Img request
    public string? SourceImageUrl { get; set; } 
    public string? ModelImageUrl { get; set; } // Model's reference photo for face/body consistency 
    
    // Optional prompt overrides
    public string? PositivePrompt { get; set; }
    public string? NegativePrompt { get; set; }
    
    public int Width { get; set; } = 1080;
    public int Height { get; set; } = 1350;
    public int NumberOfImages { get; set; } = 4;
}
