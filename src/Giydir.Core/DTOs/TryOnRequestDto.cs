namespace Giydir.Core.DTOs;

public class TryOnRequestDto
{
    public string ClothingImagePath { get; set; } = string.Empty;
    public string ModelAssetId { get; set; } = string.Empty;
    public string Category { get; set; } = "upper_body"; // upper_body, lower_body, dresses
    public int? ProjectId { get; set; }
}

