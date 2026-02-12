using System.Net.Http.Headers;
using System.Net.Http.Json;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.ExternalServices;

public class ReplicateVirtualTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly IModelAssetRepository _modelAssetRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ReplicateVirtualTryOnService> _logger;

    public ReplicateVirtualTryOnService(
        HttpClient httpClient,
        IConfiguration config,
        IModelAssetRepository modelAssetRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ReplicateVirtualTryOnService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _modelAssetRepository = modelAssetRepository;
        _httpContextAccessor = httpContextAccessor;
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
        
        // Eğer görsel zaten public URL ise (http:// veya https:// ile başlıyorsa) olduğu gibi kullan
        // Değilse BaseUrl ekle
        if (!clothingImageUrl.StartsWith("http://") && !clothingImageUrl.StartsWith("https://"))
        {
            var baseUrl = GetBaseUrl();
            if (clothingImageUrl.StartsWith("/"))
                clothingImageUrl = $"{baseUrl}{clothingImageUrl}";
        }

        if (!modelImageUrl.StartsWith("http://") && !modelImageUrl.StartsWith("https://"))
        {
            var baseUrl = GetBaseUrl();
            if (modelImageUrl.StartsWith("/"))
                modelImageUrl = $"{baseUrl}{modelImageUrl}";
        }
        
        _logger.LogInformation("Görsel URL'leri: Clothing: {ClothingUrl}, Model: {ModelUrl}", clothingImageUrl, modelImageUrl);

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
            
            // Özel hata mesajları
            if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired || 
                (errorContent.Contains("insufficient credit", StringComparison.OrdinalIgnoreCase) || 
                 errorContent.Contains("Insufficient credit", StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Replicate hesabınızda yeterli kredi bulunmuyor. Lütfen https://replicate.com/account/billing adresinden kredi satın alın. Not: Kredi satın aldıktan sonra 5-10 dakika beklemeniz gerekebilir.");
            }
            
            // Connection refused hatası - localhost URL sorunu
            if (errorContent.Contains("Connection refused") || errorContent.Contains("Failed to establish") || 
                errorContent.Contains("localhost") || errorContent.Contains("127.0.0.1"))
            {
                throw new Exception("Replicate API localhost URL'lerine erişemez! Lütfen ngrok veya cloudflare tunnel kullanarak public URL oluşturun. Detaylar için NGROK_SETUP.md dosyasına bakın.");
            }
            
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

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Replicate API response (raw): {Response}", responseContent);
        
        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();

        if (result == null)
            throw new Exception("Replicate API'den boş yanıt alındı");

        // Output null ise urls.stream veya urls.get kullan
        var outputUrl = result.Output?.FirstOrDefault();
        if (string.IsNullOrEmpty(outputUrl))
        {
            // Önce urls.stream'i dene (dosya URL'si olabilir)
            if (result.Urls?.Stream != null)
            {
                // urls.stream bir dosya URL'si olabilir, test et
                outputUrl = result.Urls.Stream;
                _logger.LogInformation("Output null, urls.stream kullanılıyor: {StreamUrl}", outputUrl);
            }
            // Eğer urls.stream de yoksa ve status succeeded ise, urls.get endpoint'ini kullan
            else if (result.Status == "succeeded" && result.Urls?.Get != null)
            {
                _logger.LogWarning("Output ve urls.stream null, urls.get endpoint'i mevcut ama kullanılamıyor: {GetUrl}", result.Urls.Get);
            }
        }

        _logger.LogInformation("Prediction status: {PredictionId} -> {Status}, OutputUrl: {OutputUrl}, OutputCount: {OutputCount}",
            predictionId, result.Status, outputUrl ?? "YOK", result.Output?.Count ?? 0);

        return new TryOnStatusResult
        {
            Status = result.Status,
            OutputUrl = outputUrl,
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

    private string GetBaseUrl()
    {
        // Önce config'den al (public URL varsa onu kullan)
        var configBaseUrl = _config["BaseUrl"];
        if (!string.IsNullOrEmpty(configBaseUrl) && !configBaseUrl.Contains("localhost"))
        {
            return configBaseUrl;
        }

        // HttpContext'ten al (request zamanında)
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var request = httpContext.Request;
            var scheme = request.Scheme;
            var host = request.Host;
            var baseUrl = $"{scheme}://{host}";
            
            // Eğer localhost ise uyarı ver
            if (host.Host.Contains("localhost") || host.Host.Contains("127.0.0.1"))
            {
                _logger.LogWarning("⚠️ UYARI: Replicate API localhost URL'lerine erişemez! Public URL gerekiyor (ngrok, cloudflare tunnel, vs.). BaseUrl: {BaseUrl}", baseUrl);
            }
            
            return baseUrl;
        }

        // Fallback: config'den veya default
        return configBaseUrl ?? "http://localhost:5267";
    }
}