namespace Giydir.Core.DTOs;

public class TryOnRequestDto
{
    public string ClothingImagePath { get; set; } = string.Empty;
    public string ModelAssetId { get; set; } = string.Empty;
    public string Category { get; set; } = "upper_body";
    public int? TemplateId { get; set; } // YENİ: Template seçildiyse
    public int? ProjectId { get; set; }
}



