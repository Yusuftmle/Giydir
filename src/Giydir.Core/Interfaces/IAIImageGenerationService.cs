using Giydir.Core.DTOs;

namespace Giydir.Core.Interfaces;

public interface IAIImageGenerationService
{
    // New Unified Engine Method
    Task<string> GenerateAsync(FashionRenderRequest request);
    
    // Status check
    Task<AIGenerationStatusResult> CheckStatusAsync(string predictionId);
    
    // Deprecated legacy method
    Task<string> GenerateImageFromPromptAsync(string prompt, string aspectRatio = "3:4", string outputFormat = "jpg", List<string>? imageInput = null, string? negativePrompt = null);
}

public class AIGenerationStatusResult
{
    public string Status { get; set; } = string.Empty;
    public string? OutputUrl { get; set; }
    public string? Error { get; set; }
}

public class ReplicatePredictionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public object? Output { get; set; }
    public string? Error { get; set; }
    public ReplicateUrls? Urls { get; set; }
}

public class ReplicateUrls
{
    public string? Get { get; set; }
    public string? Cancel { get; set; }
    public string? Stream { get; set; }
}
