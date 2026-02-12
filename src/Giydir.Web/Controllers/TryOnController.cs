using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[Route("api/[controller]")]
public class TryOnController : BaseController
{
    private readonly IVirtualTryOnService _tryOnService;
    private readonly IGeneratedImageRepository _imageRepository;
    private readonly IModelAssetRepository _modelAssetRepository;
    private readonly IAuthService _authService;
    private readonly ICreditService _creditService;
    private readonly IImageDownloadService _imageDownloadService;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<TryOnController> _logger;

    public TryOnController(
        IVirtualTryOnService tryOnService,
        IGeneratedImageRepository imageRepository,
        IModelAssetRepository modelAssetRepository,
        IAuthService authService,
        ICreditService creditService,
        IImageDownloadService imageDownloadService,
        IProjectRepository projectRepository,
        ILogger<TryOnController> logger)
    {
        _tryOnService = tryOnService;
        _imageRepository = imageRepository;
        _modelAssetRepository = modelAssetRepository;
        _authService = authService;
        _creditService = creditService;
        _imageDownloadService = imageDownloadService;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult<TryOnResponseDto>> Generate([FromBody] TryOnRequestDto request)
    {
        try
        {
            // Current user kontrolü
            var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");

            // Project kontrolü - kullanıcının kendi projesi olmalı
            if (request.ProjectId.HasValue)
            {
                var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value);
                if (project == null || project.UserId != userId)
                    return Forbid("Bu projeye erişim yetkiniz yok");
            }

            // Kredi kontrolü (manuel mod: sadece kontrol, düşürme yok)
            var creditCheck = await _creditService.CheckCreditsAsync(userId, 2);
            if (!creditCheck.HasEnough)
            {
                return BadRequest(new { error = $"Yetersiz kredi. Mevcut: {creditCheck.CurrentCredits}, Gerekli: {creditCheck.Required}" });
            }

            // Model asset kontrolü
            var modelAsset = await _modelAssetRepository.GetByIdAsync(request.ModelAssetId);
            if (modelAsset == null)
                return BadRequest(new { error = "Geçersiz model pozu seçildi" });

            // ProjectId yoksa kullanıcının ilk projesini bul veya oluştur
            int projectId;
            if (request.ProjectId.HasValue)
            {
                projectId = request.ProjectId.Value;
            }
            else
            {
                var userProjects = await _projectRepository.GetByUserIdAsync(userId);
                if (userProjects.Any())
                {
                    projectId = userProjects.First().Id;
                }
                else
                {
                    // İlk projeyi oluştur
                    var newProject = new Project
                    {
                        UserId = userId,
                        Name = "İlk Projem"
                    };
                    var createdProject = await _projectRepository.CreateAsync(newProject);
                    projectId = createdProject.Id;
                }
            }

            // Replicate API'ye istek at
            var predictionId = await _tryOnService.GenerateTryOnImageAsync(
                request.ClothingImagePath,
                request.ModelAssetId,
                request.Category);

            // DB'ye kaydet
            var generatedImage = new GeneratedImage
            {
                ProjectId = projectId,
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
    [Authorize]
    public async Task<ActionResult<TryOnStatusDto>> CheckStatus(int imageId)
    {
        try
        {
            var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");
            
            var image = await _imageRepository.GetByIdAsync(imageId);
            if (image == null)
                return NotFound(new { error = "Görsel bulunamadı" });

            // Kullanıcının kendi görseline erişim kontrolü
            var project = await _projectRepository.GetByIdAsync(image.ProjectId);
            if (project == null || project.UserId != userId)
                return Forbid("Bu görsele erişim yetkiniz yok");

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
                    // Görseli indir ve yerel olarak kaydet
                    try
                    {
                        var localPath = await _imageDownloadService.DownloadAndSaveAsync(
                            status.OutputUrl,
                            $"gen_{image.Id}_{Guid.NewGuid():N}.jpg");

                        image.Status = "Completed";
                        image.GeneratedImagePath = localPath;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Görsel indirme hatası, uzak URL kullanılıyor");
                        image.Status = "Completed";
                        image.GeneratedImagePath = status.OutputUrl;
                    }

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

    [HttpGet("download/{imageId:int}")]
    [Authorize]
    public async Task<IActionResult> DownloadImage(int imageId)
    {
        var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");
        
        var image = await _imageRepository.GetByIdAsync(imageId);
        if (image == null)
            return NotFound(new { error = "Görsel bulunamadı" });

        // Kullanıcının kendi görseline erişim kontrolü
        var project = await _projectRepository.GetByIdAsync(image.ProjectId);
        if (project == null || project.UserId != userId)
            return Forbid("Bu görsele erişim yetkiniz yok");

        if (image.Status != "Completed" || string.IsNullOrEmpty(image.GeneratedImagePath))
            return BadRequest(new { error = "Görsel henüz hazır değil" });

        // If local path, serve the file
        if (image.GeneratedImagePath.StartsWith("/uploads/"))
        {
            var webRootPath = HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>().WebRootPath;
            var filePath = Path.Combine(webRootPath, image.GeneratedImagePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
                return NotFound(new { error = "Dosya bulunamadı" });

            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(bytes, "image/jpeg", $"giydir_{imageId}.jpg");
        }

        // If remote URL, redirect
        return Redirect(image.GeneratedImagePath);
    }
}
