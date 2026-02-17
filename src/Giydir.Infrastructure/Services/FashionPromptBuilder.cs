using Giydir.Core.DTOs;
using Giydir.Core.Interfaces;

namespace Giydir.Infrastructure.Services;

public class FashionPromptBuilder : IFashionPromptBuilder
{
    private readonly IModelAssetRepository _modelRepository;

    public FashionPromptBuilder(IModelAssetRepository modelRepository)
    {
        _modelRepository = modelRepository;
    }

    public async Task<string> BuildPromptAsync(FashionRenderRequest request)
    {
        var model = await _modelRepository.GetByIdAsync(request.ModelId);
        var modelToken = model?.TriggerWord ?? "woman"; // Default trigger if not found

        // Base structure: [Model] wearing [Product] in [Vibe]
        
        var vibePrompt = GetVibeDescription(request.Vibe);
        var productDescription = $"{request.Fit} {request.Color} {request.ProductCategory}";
        
        // VTON Specifics: If source image exists, we emphasize "wearing exact garment"
        if (!string.IsNullOrEmpty(request.SourceImageUrl))
        {
             productDescription += ", exact match to reference clothing, high fidelity texture";
        }

        var prompt = $"photo of {modelToken} wearing {productDescription}, {vibePrompt}, 8k, photorealistic, masterpiece, high fashion photography, sharp focus";

        if (!string.IsNullOrEmpty(request.PositivePrompt))
        {
            prompt += $", {request.PositivePrompt}";
        }

        return prompt;
    }

    public string BuildNegativePrompt(FashionRenderRequest request)
    {
        return "illustration, painting, cartoon, low quality, blur, distorted, disfigured, text, watermark, bad anatomy, bad hands, missing fingers, extra limbs, ugly, messy";
    }

    private string GetVibeDescription(string vibe)
    {
        return vibe switch
        {
            "Studio Minimal" => "clean studio background, soft lighting, minimal aesthetic, neutral colors",
            "Urban Cinematic" => "busy city street background, cinematic lighting, depth of field, golden hour, bokeh",
            "Architectural Luxury" => "modern luxury interior, marble walls, expensive furniture, elegant lighting, architectural digest style",
            "Nature Organic" => "lush green nature background, outdoor fashion shoot, soft natural daylight, forest or garden setting",
            "Sunset Warm" => "golden hour sunset background, warm orange and purple hues, beach or horizon, silhouette lighting, romantic atmosphere",
            _ => "clean studio background, professional lighting"
        };
    }
}
