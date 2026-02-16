using System.Net.Http.Headers;
using System.Net.Http.Json;
using Giydir.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.ExternalServices;

public class NanoBananaService : IAIImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<NanoBananaService> _logger;

    public NanoBananaService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<NanoBananaService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        
        var token = _config["Replicate:ApiToken"]?.Trim();
        
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("[NanoBananaService] Nano Banana Token is NULL or EMPTY!");
        }

        _httpClient.BaseAddress = new Uri("https://api.replicate.com/v1/");

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Token {token}");
        }
        
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<string> GenerateImageFromPromptAsync(string prompt, string aspectRatio = "3:4", string outputFormat = "jpg", List<string>? imageInput = null, string? negativePrompt = null)
    {
        var modelName = _config["Replicate:NanoBananaModelVersion"] ?? "google/nano-banana-pro";

        // Replicate API'de model endpoint'ini kullan
        var endpoint = $"models/{modelName}/predictions";

        // image_input varsa aspect_ratio'yu match_input_image yap
        var finalAspectRatio = imageInput != null && imageInput.Any() ? "match_input_image" : aspectRatio;

        var inputObj = new Dictionary<string, object>
        {
            { "prompt", prompt },
            { "aspect_ratio", finalAspectRatio },
            { "output_format", outputFormat }
        };

        // Negative prompt varsa ekle
        if (!string.IsNullOrEmpty(negativePrompt))
        {
            inputObj["negative_prompt"] = negativePrompt;
        }

        // image_input varsa ekle
        if (imageInput != null && imageInput.Any())
        {
            inputObj["image_input"] = imageInput;
        }

        var payload = new
        {
            input = inputObj
        };

        _logger.LogInformation("NanoBanana API'ye istek gönderiliyor: Endpoint: {Endpoint}, Prompt: {Prompt}, AspectRatio: {AspectRatio}, OutputFormat: {OutputFormat}, ImageInputCount: {ImageInputCount}", 
            endpoint, prompt, finalAspectRatio, outputFormat, imageInput?.Count ?? 0);

        var response = await _httpClient.PostAsJsonAsync(endpoint, payload);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("NanoBanana API hatası: {StatusCode} - {Error}. Request payload: {Payload}",
                response.StatusCode, errorContent, System.Text.Json.JsonSerializer.Serialize(payload));
            
            // Özel hata mesajları
            if (response.StatusCode == System.Net.HttpStatusCode.PaymentRequired || 
                (errorContent.Contains("insufficient credit", StringComparison.OrdinalIgnoreCase) || 
                 errorContent.Contains("Insufficient credit", StringComparison.OrdinalIgnoreCase)))
            {
                throw new Exception("Replicate hesabınızda yeterli kredi bulunmuyor. Lütfen https://replicate.com/account/billing adresinden kredi satın alın. Not: Kredi satın aldıktan sonra 5-10 dakika beklemeniz gerekebilir.");
            }
            
            throw new Exception($"NanoBanana API hatası: {response.StatusCode} - {errorContent}");
        }

        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();

        if (result == null)
            throw new Exception("NanoBanana API'den boş yanıt alındı");

        _logger.LogInformation("NanoBanana prediction oluşturuldu: {PredictionId}", result.Id);

        return result.Id;
    }

    public async Task<AIGenerationStatusResult> CheckStatusAsync(string predictionId)
    {
        var response = await _httpClient.GetAsync($"predictions/{predictionId}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("NanoBanana status kontrol hatası: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new Exception($"NanoBanana API hatası: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("NanoBanana API response (raw): {Response}", responseContent);
        
        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();

        if (result == null)
            throw new Exception("NanoBanana API'den boş yanıt alındı");

        string? outputUrl = null;

        if (result.Output != null)
        {
            // JSON tipine göre işle
            if (result.Output is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    // String ise direkt al
                    outputUrl = jsonElement.GetString();
                    _logger.LogInformation("Output string olarak alındı: {Url}", outputUrl);
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Array ise ilk elemanı al
                    var firstElement = jsonElement.EnumerateArray().FirstOrDefault();
                    if (firstElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        outputUrl = firstElement.GetString();
                        _logger.LogInformation("Output array'den ilk eleman alındı: {Url}", outputUrl);
                    }
                }
            }
            else if (result.Output is string str)
            {
                outputUrl = str;
            }
        }

        _logger.LogInformation("NanoBanana prediction status: {PredictionId} -> {Status}, OutputUrl: {OutputUrl}",
            predictionId, result.Status, outputUrl ?? "YOK");

        return new AIGenerationStatusResult
        {
            Status = result.Status,
            OutputUrl = outputUrl,
            Error = result.Error
        };
    }
}

