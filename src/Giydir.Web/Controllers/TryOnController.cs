using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TryOnController : ControllerBase
{
    private readonly IVirtualTryOnService _tryOnService;
    private readonly IGeneratedImageRepository _imageRepository;
    private readonly IModelAssetRepository _modelAssetRepository;
    private readonly ILogger<TryOnController> _logger;

    public TryOnController(
        IVirtualTryOnService tryOnService,
        IGeneratedImageRepository imageRepository,
        IModelAssetRepository modelAssetRepository,
        ILogger<TryOnController> logger)
    {
        _tryOnService = tryOnService;
        _imageRepository = imageRepository;
        _modelAssetRepository = modelAssetRepository;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<TryOnResponseDto>> Generate([FromBody] TryOnRequestDto request)
    {
        try
        {
            // Model asset kontrolü
            var modelAsset = await _modelAssetRepository.GetByIdAsync(request.ModelAssetId);
            if (modelAsset == null)
                return BadRequest(new { error = "Geçersiz model pozu seçildi" });

            // Replicate API'ye istek at
            var predictionId = await _tryOnService.GenerateTryOnImageAsync(
                request.ClothingImagePath,
                request.ModelAssetId,
                request.Category);

            // DB'ye kaydet
            var generatedImage = new GeneratedImage
            {
                ProjectId = request.ProjectId ?? 1, // Default project
                OriginalClothingPath = request.ClothingImagePath,
                ModelAssetId = request.ModelAssetId,
                ReplicatePredictionId = predictionId,
                Status = "Processing"
            };

            await _imageRepository.CreateAsync(generatedImage);

            return Ok(new TryOnResponseDto
            {
                ImageId = generatedImage.Id,
                PredictionId = predictionId,
                Status = "Processing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Try-on oluşturma hatası");
            return StatusCode(500, new { error = "Görsel oluşturulurken bir hata oluştu. Lütfen tekrar deneyin." });
        }
    }

    [HttpGet("status/{imageId:int}")]
    public async Task<ActionResult<TryOnStatusDto>> CheckStatus(int imageId)
    {
        try
        {
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
                return NotFound(new { error = "Görsel bulunamadı" });

            // Zaten tamamlanmış veya başarısız ise direkt dön
            if (image.Status == "Completed" || image.Status == "Failed")
            {
                return Ok(new TryOnStatusDto
                {
                    ImageId = image.Id,
                    Status = image.Status,
                    GeneratedImageUrl = image.GeneratedImagePath,
                    ErrorMessage = image.ErrorMessage
                });
            }

            // Replicate'den durum kontrol et
            if (!string.IsNullOrEmpty(image.ReplicatePredictionId))
            {
                var status = await _tryOnService.CheckStatusAsync(image.ReplicatePredictionId);

                if (status.Status == "succeeded" && !string.IsNullOrEmpty(status.OutputUrl))
                {
                    image.Status = "Completed";
                    image.GeneratedImagePath = status.OutputUrl;
                    await _imageRepository.UpdateAsync(image);
                }
                else if (status.Status == "failed")
                {
                    image.Status = "Failed";
                    image.ErrorMessage = status.Error ?? "AI görsel oluşturma başarısız oldu";
                    await _imageRepository.UpdateAsync(image);
                }
            }

            return Ok(new TryOnStatusDto
            {
                ImageId = image.Id,
                Status = image.Status,
                GeneratedImageUrl = image.GeneratedImagePath,
                ErrorMessage = image.ErrorMessage
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Status kontrol hatası: ImageId={ImageId}", imageId);
            return StatusCode(500, new { error = "Durum kontrolü sırasında bir hata oluştu" });
        }
    }
}

