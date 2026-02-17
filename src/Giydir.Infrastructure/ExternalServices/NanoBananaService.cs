using System.Net.Http.Headers;
using System.Net.Http.Json;
using Giydir.Core.DTOs;
using Giydir.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;

namespace Giydir.Infrastructure.ExternalServices;

public class NanoBananaService : IAIImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<NanoBananaService> _logger;
    private readonly IFashionPromptBuilder _promptBuilder;

    public NanoBananaService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<NanoBananaService> logger,
        IFashionPromptBuilder promptBuilder)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
        _promptBuilder = promptBuilder;
        
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

    public async Task<string> GenerateAsync(FashionRenderRequest request)
    {
        // Use specific hash if available, fallback to alias
        var modelName = _config["Replicate:ModelVersion"] ?? _config["Replicate:NanoBananaModelVersion"] ?? "google/nano-banana-pro";
        var prompt = await _promptBuilder.BuildPromptAsync(request);
        var negativePrompt = _promptBuilder.BuildNegativePrompt(request);

        string endpoint;
        object payload;

        var inputObj = new Dictionary<string, object>
        {
            { "prompt", prompt },
            { "aspect_ratio", "3:4" }, // Default for fashion
            { "output_format", "jpg" },
            { "resolution", "2K" },
            { "safety_filter_level", "block_only_high" },
            { "negative_prompt", negativePrompt }
        };

        string? compositeUri = null;

        // Composite Input Logic (Method 1)
        if (!string.IsNullOrEmpty(request.SourceImageUrl) && !string.IsNullOrEmpty(request.ModelImageUrl))
        {
            try 
            {
                compositeUri = await CreateCompositeImageAsync(request.ModelImageUrl, request.SourceImageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Composite image generation failed. Falling back to single image.");
            }
        }

        if (compositeUri != null)
        {
             inputObj["image_input"] = new[] { compositeUri }; // Changed to array
             
             // Update prompt for composite workflow with production-grade instructions requested by user
             var compositePrompt = $"{prompt} . The person in the image must wear the exact jacket shown in the reference garment image. Preserve the original face, pose, body proportions and background. Replace only the upper body clothing with the exact garment from the reference. The garment must match color, texture, fabric details and fit realistically to the body. Do not change the person identity. Do not change lighting. Do not generate a different person. Do not change background. Only apply the garment naturally as if worn. Photorealistic, high detail, realistic fabric folds, accurate shadows.";
             inputObj["prompt"] = compositePrompt;
             
             // Enhanced negative prompt
             var enhancedNegative = $"{inputObj["negative_prompt"]}, distorted body, extra limbs, blurry, warped torso, deformed hands, changes in background, different face, cartoon, illustration";
             inputObj["negative_prompt"] = enhancedNegative;
        }
        else if (!string.IsNullOrEmpty(request.SourceImageUrl))
        {
            // Fallback / Standard single image (Clothing only)
            var uri = await GetBase64DataUriAsync(request.SourceImageUrl);
            if (uri != null) 
            {
                 inputObj["image_input"] = new[] { uri }; // Changed to array
            }
        }
        
        // Tuned parameters for better reference adherence
        inputObj["strength"] = 0.4; // User requested 0.35 - 0.45
        inputObj["guidance_scale"] = 8; // User requested 7 - 9

        // Explicitly remove VTON keys and old keys to ensure schema compliance
        if (inputObj.ContainsKey("human_img")) inputObj.Remove("human_img");
        if (inputObj.ContainsKey("garm_img")) inputObj.Remove("garm_img");
        if (inputObj.ContainsKey("garment_des")) inputObj.Remove("garment_des");
        if (inputObj.ContainsKey("category")) inputObj.Remove("category");
        if (inputObj.ContainsKey("image")) inputObj.Remove("image"); // Remove old key

        // Use specific hash if available... (logic continues)
        // Check if modelName is 'owner/name' or just a version hash
        if (modelName.Contains("/"))
        {
            endpoint = $"models/{modelName}/predictions";
            // For owner/name endpoint, payload structure is slightly different sometimes, but usually { input: ... } works.
            payload = new { input = inputObj };
        }
        else
        {
            endpoint = "predictions";
            payload = new { version = modelName, input = inputObj };
        }

        _logger.LogInformation("NanoBanana API Request: {Prompt}", prompt);

        // Retry loop for rate limiting (429) & Service Unavailable (503/E003)
        const int maxRetries = 5; // Increased attempts
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, payload);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests || 
                response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                if (attempt < maxRetries)
                {
                    var waitSeconds = 5 * (attempt + 1); // 5s, 10s, 15s...
                    _logger.LogWarning("API Busy/Error ({Status}). Retrying in {Wait}s (attempt {Attempt}/{Max})...", 
                        response.StatusCode, waitSeconds, attempt + 1, maxRetries);
                    await Task.Delay(waitSeconds * 1000);
                    continue;
                }
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("NanoBanana API Error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new Exception($"NanoBanana API Error: {response.StatusCode} - {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();
            if (result == null) throw new Exception("Empty response from NanoBanana API");

            _logger.LogInformation("NanoBanana prediction created: {PredictionId}", result.Id);
            return result.Id;
        }

        throw new Exception("NanoBanana API: max retries exceeded due to rate limiting");
    }

    // Checking status remains same
    private async Task<string?> GetBase64DataUriAsync(string imageUrl)
    {
        // If it's already a URL, return as is
        if (imageUrl.StartsWith("http://") || imageUrl.StartsWith("https://"))
        {
             return imageUrl;
        }
        
        // Resolve the local file path (e.g., "/uploads/xxx.webp" → "wwwroot/uploads/xxx.webp")
        var localPath = imageUrl.TrimStart('/');
        var fullPath = Path.Combine("wwwroot", localPath);

        if (File.Exists(fullPath))
        {
            try 
            {
                var fileBytes = await File.ReadAllBytesAsync(fullPath);
                var base64 = Convert.ToBase64String(fileBytes);
                var ext = Path.GetExtension(fullPath).TrimStart('.').ToLower();
                var mimeType = ext switch
                {
                    "jpg" or "jpeg" => "image/jpeg",
                    "png" => "image/png",
                    "webp" => "image/webp",
                    _ => "image/jpeg"
                };
                _logger.LogInformation("Converted local image to base64 data URI ({Size} bytes): {Path}", fileBytes.Length, imageUrl);
                return $"data:{mimeType};base64,{base64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting image to base64: {Path}", fullPath);
                return null;
            }
        }
        else
        {
            _logger.LogWarning("Local image file not found: {Path}", fullPath);
            return null;
        }
    }

    // Image Composition Helper (Method 1 - Production Ready)
    private async Task<string?> CreateCompositeImageAsync(string modelPath, string clothingPath)
    {
        try
        {
            using var modelImg = await LoadImageFromPathAsync(modelPath);
            using var clothImg = await LoadImageFromPathAsync(clothingPath);

            if (modelImg == null || clothImg == null) return null;

            // Canvas Layout: 1024x1024
            // Split: 60% Model (Left), 40% Clothing (Right)
            // Gap: 30px
            
            int totalWidth = 1024;
            int totalHeight = 1024;
            int gap = 30;
            
            int remainingWidth = totalWidth - gap;
            int modelWidth = (int)(remainingWidth * 0.6); // ~596px
            int clothWidth = remainingWidth - modelWidth; // ~398px
            
            // Resize Model: Crop to fill 60% width, full height
            modelImg.Mutate(x => x.Resize(new ResizeOptions 
            {
                Size = new Size(modelWidth, totalHeight),
                Mode = ResizeMode.Crop,
                Position = AnchorPositionMode.Center
            }));

            // Resize Clothing: Pad to fit 40% width, full height (preserve aspect ratio)
            // Use White background for padding to reduce clutter
            clothImg.Mutate(x => x.Resize(new ResizeOptions 
            {
                Size = new Size(clothWidth, totalHeight),
                Mode = ResizeMode.Pad,
                PadColor = Color.White
            }));

            using var outputImage = new Image<Rgba32>(totalWidth, totalHeight);
            
            // Fill background White
            outputImage.Mutate(ctx => 
            {
                ctx.Fill(Color.White);
                ctx.DrawImage(modelImg, new Point(0, 0), 1f); // Left: Model
                ctx.DrawImage(clothImg, new Point(modelWidth + gap, 0), 1f); // Right: Clothing
            });

            using var ms = new MemoryStream();
            await outputImage.SaveAsJpegAsync(ms);
            var base64 = Convert.ToBase64String(ms.ToArray());
            return $"data:image/jpeg;base64,{base64}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating composite image");
            return null;
        }
    }

    private async Task<Image?> LoadImageFromPathAsync(string path)
    {
        if (path.StartsWith("http")) return null; // Not supported currently for simplicity, assuming local

        var localPath = path.TrimStart('/');
        var fullPath = Path.Combine("wwwroot", localPath);

        if (!File.Exists(fullPath)) return null;

        var bytes = await File.ReadAllBytesAsync(fullPath);
        return Image.Load(bytes);
    }

    public async Task<AIGenerationStatusResult> CheckStatusAsync(string predictionId)
    {
        var response = await _httpClient.GetAsync($"predictions/{predictionId}");

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("NanoBanana status error: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new Exception($"NanoBanana API Error: {response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();

        if (result == null)
            throw new Exception("Empty response from NanoBanana API");

        string? outputUrl = null;

        if (result.Output != null)
        {
            if (result.Output is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    outputUrl = jsonElement.GetString();
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    var firstElement = jsonElement.EnumerateArray().FirstOrDefault();
                    if (firstElement.ValueKind == System.Text.Json.JsonValueKind.String)
                    {
                        outputUrl = firstElement.GetString();
                    }
                }
            }
            else if (result.Output is string str)
            {
                outputUrl = str;
            }
        }

        return new AIGenerationStatusResult
        {
            Status = result.Status,
            OutputUrl = outputUrl,
            Error = result.Error
        };
    }

    // Keep legacy interface method for compatibility until refactor is complete
    // But direct it to new logic if possible or throw not implemented if we want to force migration
    public Task<string> GenerateImageFromPromptAsync(string prompt, string aspectRatio = "3:4", string outputFormat = "jpg", List<string>? imageInput = null, string? negativePrompt = null)
    {
         throw new NotImplementedException("Use GenerateAsync(FashionRenderRequest) instead.");
    }


}




