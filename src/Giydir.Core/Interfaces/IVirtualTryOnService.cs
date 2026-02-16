namespace Giydir.Core.Interfaces;

public interface IVirtualTryOnService
{
    Task<string> GenerateTryOnImageAsync(string clothingImageUrl, string modelAssetId, string category = "upper_body", string? background = null, string? lighting = null);
    Task<TryOnStatusResult> CheckStatusAsync(string predictionId);
}

public class TryOnStatusResult
{
    public string Status { get; set; } = string.Empty; // starting, processing, succeeded, failed
    public string? OutputUrl { get; set; }
    public string? Error { get; set; }
}




