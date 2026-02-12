namespace Giydir.Core.Entities;

public class GeneratedImage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string OriginalClothingPath { get; set; } = string.Empty;
    public string ModelAssetId { get; set; } = string.Empty; // Hangi model pozu kullanıldı
    public string? GeneratedImagePath { get; set; }
    public string? ReplicatePredictionId { get; set; }
    public string Status { get; set; } = "Processing"; // Processing, Completed, Failed
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public ModelAsset ModelAsset { get; set; } = null!;
}

