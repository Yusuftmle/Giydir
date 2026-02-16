namespace Giydir.Core.DTOs;

public class TryOnRequestDto
{
    public string ClothingImagePath { get; set; } = string.Empty;
    public string ModelAssetId { get; set; } = string.Empty;
    public string Category { get; set; } = "upper_body";
    public int? TemplateId { get; set; } 
    public int? ProjectId { get; set; }
    public string? Background { get; set; }
    public string? Lighting { get; set; }
    public bool Enhance { get; set; }
    public bool Retouch { get; set; }
}



