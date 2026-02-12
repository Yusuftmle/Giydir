using System.Net.Http.Headers;
using System.Net.Http.Json;
using Giydir.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.ExternalServices;

public class ReplicateVirtualTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IModelAssetRepository _modelAssetRepository;
    private readonly ILogger<ReplicateVirtualTryOnService> _logger;

    public ReplicateVirtualTryOnService(
        HttpClient httpClient,
        IConfiguration config,
        IModelAssetRepository modelAssetRepository,
        ILogger<ReplicateVirtualTryOnService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _modelAssetRepository = modelAssetRepository;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.replicate.com/v1/");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Token", _config["Replicate:ApiToken"]);
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GenerateTryOnImageAsync(string clothingImageUrl, string modelAssetId, string category = "upper_body")
    {
        // Model asset'inden gerçek URL'yi al (veritabanından)
        var modelImageUrl = await GetModelImageUrlAsync(modelAssetId);
        var baseUrl = _config["BaseUrl"] ?? "http://localhost:5000";

        // Eğer relative path ise full URL yap
        if (clothingImageUrl.StartsWith("/"))
            clothingImageUrl = $"{baseUrl}{clothingImageUrl}";

        if (modelImageUrl.StartsWith("/"))
            modelImageUrl = $"{baseUrl}{modelImageUrl}";

        var payload = new
        {
            version = _config["Replicate:ModelVersion"],
            input = new
            {
                garm_img = clothingImageUrl,
                human_img = modelImageUrl,
                garment_des = "clothing item",
                category = category
            }
        };

        _logger.LogInformation("Replicate API'ye istek gönderiliyor: {ModelAssetId}, Kategori: {Category}",
            modelAssetId, category);

        var response = await _httpClient.PostAsJsonAsync("predictions", payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Replicate API hatası: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new Exception($"Replicate API hatası: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();

        if (result == null)
            throw new Exception("Replicate API'den boş yanıt alındı");

        _logger.LogInformation("Prediction oluşturuldu: {PredictionId}", result.Id);

        return result.Id;
    }

    public async Task<TryOnStatusResult> CheckStatusAsync(string predictionId)
    {
        var response = await _httpClient.GetAsync($"predictions/{predictionId}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Replicate status kontrol hatası: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new Exception($"Replicate API hatası: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();

        if (result == null)
            throw new Exception("Replicate API'den boş yanıt alındı");

        _logger.LogInformation("Prediction status: {PredictionId} -> {Status}",
            predictionId, result.Status);

        return new TryOnStatusResult
        {
            Status = result.Status,
            OutputUrl = result.Output?.FirstOrDefault(),
            Error = result.Error
        };
    }

    private async Task<string> GetModelImageUrlAsync(string modelAssetId)
    {
        var modelAsset = await _modelAssetRepository.GetByIdAsync(modelAssetId);
        if (modelAsset != null && !string.IsNullOrEmpty(modelAsset.FullImagePath))
        {
            return modelAsset.FullImagePath;
        }

        // Fallback: eski yöntem
        _logger.LogWarning("Model asset bulunamadı veya FullImagePath boş: {ModelAssetId}", modelAssetId);
        return $"/assets/models/{modelAssetId}.jpg";
    }
}

internal class ReplicatePredictionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // starting, processing, succeeded, failed, canceled
    public List<string>? Output { get; set; }
    public string? Error { get; set; }
}
