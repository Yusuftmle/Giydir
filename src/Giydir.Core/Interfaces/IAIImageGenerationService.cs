namespace Giydir.Core.Interfaces;

public interface IAIImageGenerationService
{
    /// <summary>
    /// Text prompt'tan görsel oluşturur (nano-banana gibi modeller için)
    /// </summary>
    /// <param name="prompt">Görsel açıklaması</param>
    /// <param name="aspectRatio">Görsel oranı (default: 4:3, image_input varsa match_input_image)</param>
    /// <param name="outputFormat">Çıktı formatı (default: jpg)</param>
    /// <param name="imageInput">Opsiyonel referans görseller (URL listesi)</param>
    Task<string> GenerateImageFromPromptAsync(string prompt, string aspectRatio = "4:3", string outputFormat = "jpg", List<string>? imageInput = null);
    
    /// <summary>
    /// Prediction durumunu kontrol eder
    /// </summary>
    Task<AIGenerationStatusResult> CheckStatusAsync(string predictionId);
}

public class AIGenerationStatusResult
{
    public string Status { get; set; } = string.Empty; // starting, processing, succeeded, failed
    public string? OutputUrl { get; set; }
    public string? Error { get; set; }
}

