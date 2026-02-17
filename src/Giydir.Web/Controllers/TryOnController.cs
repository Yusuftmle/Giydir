using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[Route("api/[controller]")]
public class TryOnController : BaseController
{
    private readonly IGeneratedImageRepository _imageRepository;
    private readonly IModelAssetRepository _modelAssetRepository;
    private readonly IAuthService _authService;
    private readonly ICreditService _creditService;
    private readonly IImageDownloadService _imageDownloadService;
    private readonly IProjectRepository _projectRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IPromptService _promptService;
    private readonly IAIImageGenerationService _aiImageService;
    private readonly ILogger<TryOnController> _logger;

    public TryOnController(
        IGeneratedImageRepository imageRepository,
        IModelAssetRepository modelAssetRepository,
        IAuthService authService,
        ICreditService creditService,
        IImageDownloadService imageDownloadService,
        IProjectRepository projectRepository,
        ITemplateRepository templateRepository,
        IPromptService promptService,
        IAIImageGenerationService aiImageService,
        ILogger<TryOnController> logger)
    {
        _imageRepository = imageRepository;
        _modelAssetRepository = modelAssetRepository;
        _authService = authService;
        _creditService = creditService;
        _imageDownloadService = imageDownloadService;
        _projectRepository = projectRepository;
        _templateRepository = templateRepository;
        _promptService = promptService;
        _aiImageService = aiImageService;
        _logger = logger;
    }

    [HttpPost("generate")]
    [Authorize]
    public async Task<ActionResult> Generate([FromBody] GenerateRequestDto request)
    {
        try
        {
            var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");

            // Template kontrolü - Template varsa öncelikli
            Template? template = null;
            if (request.TemplateId.HasValue)
            {
                template = await _templateRepository.GetByIdAsync(request.TemplateId.Value);
                if (template == null || !template.IsActive)
                    return BadRequest(new { error = "Geçersiz template seçildi" });
            }

            // Project kontrolü
            if (request.ProjectId.HasValue)
            {
                var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value);
                if (project == null || project.UserId != userId)
                    return Forbid("Bu projeye erişim yetkiniz yok");
            }

            // Kredi kontrolü
            var creditCheck = await _creditService.CheckCreditsAsync(userId, 2);
            if (!creditCheck.HasEnough)
            {
                return BadRequest(new { error = $"Yetersiz kredi. Mevcut: {creditCheck.CurrentCredits}, Gerekli: {creditCheck.Required}" });
            }

            // Model asset kontrolü
            ModelAsset? modelAsset = null;
            if (!string.IsNullOrEmpty(request.ModelAssetId))
            {
                modelAsset = await _modelAssetRepository.GetByIdAsync(request.ModelAssetId);
                if (modelAsset == null)
                    return BadRequest(new { error = "Geçersiz model pozu seçildi" });
            }

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
                    var newProject = new Project
                    {
                        UserId = userId,
                        Name = "İlk Projem"
                    };
                    var createdProject = await _projectRepository.CreateAsync(newProject);
                    projectId = createdProject.Id;
                }
            }

            // Unified NanoBanana Engine - all generation goes through AI service
            string predictionId;

            if (template != null)
            {
                // Template seçildiyse AI generation yap (template özellikleri kullanılacak)
                var prompt = _promptService.GeneratePromptWithModelDefaults(
                    template,
                    modelAsset,
                    template.Style,
                    template.Color,
                    template.Pattern,
                    template.Material,
                    template.Category
                );
                
                _logger.LogInformation("Template ile prompt oluşturuldu: {TemplateId} -> {Prompt}", template.Id, prompt);

                predictionId = await _aiImageService.GenerateAsync(new FashionRenderRequest
                {
                    ProductCategory = template.Category,
                    Fit = "regular",
                    Color = template.Color ?? "original",
                    Vibe = "Studio Minimal",
                    ModelId = modelAsset?.Id ?? "default",
                    SourceImageUrl = request.ClothingImagePath,
                    PositivePrompt = prompt
                });
            }
            else
            {
                // Unified NanoBanana generation (replaces legacy VTON)
                if (modelAsset == null)
                    return BadRequest(new { error = "Model veya template seçilmelidir" });

                predictionId = await _aiImageService.GenerateAsync(new FashionRenderRequest
                {
                    ProductCategory = request.Category ?? "upper_body",
                    Fit = "regular",
                    Color = "original",
                    Vibe = request.Background ?? "Studio Minimal",
                    ModelId = request.ModelAssetId!,
                    SourceImageUrl = request.ClothingImagePath,
                    PositivePrompt = $"lighting {request.Lighting ?? "Studio Soft"}"
                });
            }

            var generatedImage = new GeneratedImage
            {
                ProjectId = projectId,
                OriginalClothingPath = request.ClothingImagePath,
                ModelAssetId = modelAsset?.Id ?? "ai-generated",
                ReplicatePredictionId = predictionId,
                Status = "Processing"
            };

            await _imageRepository.CreateAsync(generatedImage);

            return Ok(new
            {
                ImageId = generatedImage.Id,
                PredictionId = predictionId,
                Status = "Processing"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görsel oluşturma hatası: {Message}", ex.Message);
            var errorMessage = ex.Message;
            if (errorMessage.Contains("yeterli kredi") || errorMessage.Contains("insufficient credit"))
            {
                return StatusCode(402, new { error = errorMessage });
            }
            return StatusCode(500, new { error = $"Görsel oluşturulurken bir hata oluştu: {errorMessage}" });
        }
    }

    [HttpGet("status/{imageId:int}")]
    [Authorize]
    public async Task<ActionResult> CheckStatus(int imageId)
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
                return Ok(new
                {
                    ImageId = image.Id,
                    Status = image.Status,
                    GeneratedImageUrl = image.GeneratedImagePath,
                    ErrorMessage = image.ErrorMessage
                });
            }

            // Replicate'den durum kontrol et (Unified NanoBanana Engine)
            if (!string.IsNullOrEmpty(image.ReplicatePredictionId))
            {
                _logger.LogInformation("Status kontrol: ImageId={ImageId}, PredictionId={PredictionId}, MevcutStatus={Status}",
                    imageId, image.ReplicatePredictionId, image.Status);
                
                var status = await _aiImageService.CheckStatusAsync(image.ReplicatePredictionId);
                
                _logger.LogInformation("Replicate status: ImageId={ImageId}, Status={Status}, OutputUrl={OutputUrl}",
                    imageId, status.Status, status.OutputUrl ?? "YOK");

                if (status.Status == "succeeded" && !string.IsNullOrEmpty(status.OutputUrl))
                {
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
                    _logger.LogWarning("Görsel başarısız: ImageId={ImageId}, Error={Error}", imageId, image.ErrorMessage);
                }
                else
                {
                    _logger.LogInformation("Görsel hala işleniyor: ImageId={ImageId}, Status={Status}", 
                        imageId, status.Status);
                }
            }

            return Ok(new
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

    [HttpPost("generate-from-template")]
    [Authorize]
    public async Task<ActionResult<AIGenerationResponseDto>> GenerateFromTemplate([FromBody] GenerateFromTemplateDto request)
    {
        try
        {
            var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");

            // Template kontrolü
            var template = await _templateRepository.GetByIdAsync(request.TemplateId);
            if (template == null || !template.IsActive)
                return BadRequest(new AIGenerationResponseDto { Success = false, Error = "Geçersiz template seçildi" });

            // Kredi kontrolü
            var creditCheck = await _creditService.CheckCreditsAsync(userId, 2);
            if (!creditCheck.HasEnough)
            {
                return BadRequest(new AIGenerationResponseDto 
                { 
                    Success = false, 
                    Error = $"Yetersiz kredi. Mevcut: {creditCheck.CurrentCredits}, Gerekli: {creditCheck.Required}" 
                });
            }

            // ProjectId yoksa kullanıcının ilk projesini bul veya oluştur
            int projectId;
            if (request.ProjectId.HasValue)
            {
                var project = await _projectRepository.GetByIdAsync(request.ProjectId.Value);
                if (project == null || project.UserId != userId)
                    return Forbid("Bu projeye erişim yetkiniz yok");
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
                    var newProject = new Project
                    {
                        UserId = userId,
                        Name = "İlk Projem"
                    };
                    var createdProject = await _projectRepository.CreateAsync(newProject);
                    projectId = createdProject.Id;
                }
            }

            // Template'den prompt oluştur
            var prompt = _promptService.GeneratePromptFromTemplate(template, request.CustomPrompt);
            _logger.LogInformation("Template'den prompt oluşturuldu: {TemplateId} -> {Prompt}", template.Id, prompt);

            // NanoBanana Unified Engine
            var predictionId = await _aiImageService.GenerateAsync(new FashionRenderRequest
            {
                ProductCategory = template.Category,
                Fit = "regular",
                Color = template.Color ?? "original",
                Vibe = "Studio Minimal",
                ModelId = "default",
                PositivePrompt = prompt
            });

            // DB'ye kaydet
            var generatedImage = new GeneratedImage
            {
                ProjectId = projectId,
                OriginalClothingPath = $"template_{template.Id}", // Template ID'si
                ModelAssetId = "ai-generated", // AI generated için özel ID
                ReplicatePredictionId = predictionId,
                Status = "Processing"
            };

            await _imageRepository.CreateAsync(generatedImage);

            return Ok(new AIGenerationResponseDto
            {
                Success = true,
                ImageId = generatedImage.Id,
                PredictionId = predictionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template-based görsel oluşturma hatası: {Message}", ex.Message);
            _logger.LogError(ex, "Stack trace: {StackTrace}", ex.StackTrace);
            
            var errorMessage = ex.Message;
            if (ex.InnerException != null)
            {
                errorMessage += $" | Inner: {ex.InnerException.Message}";
            }
            
            // Kredi hatası gibi özel durumlar için daha açıklayıcı mesaj
            var statusCode = 500;
            if (errorMessage.Contains("yeterli kredi") || errorMessage.Contains("insufficient credit"))
            {
                statusCode = 402;
            }
            
            return StatusCode(statusCode, new AIGenerationResponseDto 
            { 
                Success = false, 
                Error = $"Görsel oluşturulurken bir hata oluştu: {errorMessage}" 
            });
        }
    }

    [HttpPost("save-selection")]
    [Authorize]
    public async Task<ActionResult> SaveSelection([FromBody] SaveSelectionDto request)
    {
        try
        {
            var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");

            if (request.ImageUrls == null || !request.ImageUrls.Any())
                return BadRequest(new { error = "Kaydedilecek görsel seçilmedi" });

            // Find or create project
            int projectId;
            var userProjects = await _projectRepository.GetByUserIdAsync(userId);
            if (userProjects.Any())
            {
                projectId = userProjects.First().Id;
            }
            else
            {
                var newProject = new Project
                {
                    UserId = userId,
                    Name = "İlk Projem"
                };
                var createdProject = await _projectRepository.CreateAsync(newProject);
                projectId = createdProject.Id;
            }

            var savedImages = new List<object>();

            foreach (var imageUrl in request.ImageUrls)
            {
                try
                {
                    // Download image locally
                    string localPath;
                    try
                    {
                        localPath = await _imageDownloadService.DownloadAndSaveAsync(
                            imageUrl,
                            $"saved_{Guid.NewGuid():N}.jpg");
                    }
                    catch
                    {
                        // If download fails, keep the remote URL
                        localPath = imageUrl;
                    }

                    var generatedImage = new GeneratedImage
                    {
                        ProjectId = projectId,
                        OriginalClothingPath = "editor-selection",
                        ModelAssetId = "editor-saved",
                        GeneratedImagePath = localPath,
                        Status = "Completed"
                    };

                    await _imageRepository.CreateAsync(generatedImage);
                    savedImages.Add(new { ImageId = generatedImage.Id, Path = localPath });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Görsel kaydedilemedi: {Url}", imageUrl);
                }
            }

            _logger.LogInformation("Seçim kaydedildi: {Count} görsel, Proje: {ProjectId}", savedImages.Count, projectId);

            return Ok(new { saved = savedImages.Count, projectId, images = savedImages });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Seçim kaydetme hatası");
            return StatusCode(500, new { error = "Seçim kaydedilirken bir hata oluştu" });
        }
    }
}

/// <summary>
/// Unified request DTO for the generate endpoint (replaces legacy TryOnRequestDto)
/// </summary>
public class GenerateRequestDto
{
    public string ClothingImagePath { get; set; } = string.Empty;
    public string? ModelAssetId { get; set; }
    public string? Category { get; set; }
    public string? Background { get; set; }
    public string? Lighting { get; set; }
    public int? TemplateId { get; set; }
    public int? ProjectId { get; set; }
}

public class SaveSelectionDto
{
    public List<string> ImageUrls { get; set; } = new();
}
