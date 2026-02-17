using Giydir.Core.DTOs;
using Giydir.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.Services;

public class RenderOrchestrator
{
    private readonly IAIImageGenerationService _generationService;
    private readonly ILogger<RenderOrchestrator> _logger;

    public RenderOrchestrator(
        IAIImageGenerationService generationService,
        ILogger<RenderOrchestrator> logger)
    {
        _generationService = generationService;
        _logger = logger;
    }

    public async Task<List<string>> GenerateFashionSetAsync(FashionRenderRequest baseRequest)
    {
        _logger.LogInformation("Starting Fashion Set Generation for Model: {ModelId}, Count: {Count}", baseRequest.ModelId, baseRequest.NumberOfImages);

        var predictionIds = new List<string>();
        int count = Math.Max(1, Math.Min(4, baseRequest.NumberOfImages)); // Ensure 1-4 range

        for (int i = 0; i < count; i++)
        {
            try
            {
                var request = CloneRequest(baseRequest);
                
                // We rely on the model's random seed for variations. 
                // We do NOT modify the prompt/vibe to ensure strict adherence to user settings.

                var predictionId = await _generationService.GenerateAsync(request);
                predictionIds.Add(predictionId);
                _logger.LogInformation("Variation {Number} generated: {Vibe}, PredictionId={PredictionId}", i + 1, request.Vibe, predictionId);

                // Wait 12s between requests to avoid rate limiting
                if (i < count - 1)
                    await Task.Delay(12_000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Variation {Number} failed, skipping...", i + 1);
            }
        }

        return predictionIds;
    }

    private FashionRenderRequest CloneRequest(FashionRenderRequest original)
    {
        return new FashionRenderRequest
        {
            ProductCategory = original.ProductCategory,
            Fit = original.Fit,
            Color = original.Color,
            Vibe = original.Vibe,
            ModelId = original.ModelId,
            SourceImageUrl = original.SourceImageUrl,
            PositivePrompt = original.PositivePrompt,
            NegativePrompt = original.NegativePrompt,
            Width = original.Width,
            Height = original.Height,
            NumberOfImages = original.NumberOfImages,
            ModelImageUrl = original.ModelImageUrl
        };
    }
}
