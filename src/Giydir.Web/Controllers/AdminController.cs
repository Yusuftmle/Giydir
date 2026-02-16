using System.Security.Claims;
using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAIImageGenerationService _aiImageService;
    private readonly ISavedPromptRepository _promptRepository;
    private readonly IModelAssetRepository _modelRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAIImageGenerationService aiImageService,
        ISavedPromptRepository promptRepository,
        IModelAssetRepository modelRepository,
        IAuthService authService,
        ILogger<AdminController> logger)
    {
        _aiImageService = aiImageService;
        _promptRepository = promptRepository;
        _modelRepository = modelRepository;
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("generate-model")]
    public async Task<IActionResult> GenerateModel([FromBody] AdminGenerateModelRequestDto request)
    {
        try
        {
            var predictionId = await _aiImageService.GenerateImageFromPromptAsync(request.Prompt, request.AspectRatio, "jpg", null, request.NegativePrompt);
            return Ok(new { PredictionId = predictionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin model generation error");
            return StatusCode(500, "Model üretilirken bir hata oluştu.");
        }
    }

    [HttpGet("generation-status/{predictionId}")]
    public async Task<IActionResult> GetStatus(string predictionId)
    {
        var result = await _aiImageService.CheckStatusAsync(predictionId);
        return Ok(result);
    }

    [HttpGet("prompts")]
    public async Task<IActionResult> GetSavedPrompts()
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var prompts = await _promptRepository.GetByUserIdAsync(userId.Value);
        return Ok(prompts);
    }

    [HttpPost("prompts")]
    public async Task<IActionResult> SavePrompt([FromBody] AdminSavePromptRequestDto request)
    {
        var userId = _authService.GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var savedPrompt = new SavedPrompt
        {
            Name = request.Name,
            PromptText = request.PromptText,
            NegativePrompt = request.NegativePrompt,
            ResultImageUrl = request.ResultImageUrl,
            SettingsJson = request.SettingsJson,
            CreatedByUserId = userId.Value,
            CreatedAt = DateTime.UtcNow
        };

        await _promptRepository.CreateAsync(savedPrompt);
        return Ok(savedPrompt);
    }

    [HttpDelete("prompts/{id}")]
    public async Task<IActionResult> DeletePrompt(int id)
    {
        await _promptRepository.DeleteAsync(id);
        return Ok();
    }

    [HttpPost("publish-model")]
    public async Task<IActionResult> PublishModel([FromBody] AdminPublishModelRequestDto request)
    {
        var modelAsset = new ModelAsset
        {
            Id = Guid.NewGuid().ToString().Substring(0, 8),
            Name = request.Name,
            FullImagePath = request.ImageUrl,
            ThumbnailPath = request.ImageUrl, // Basitleştirilmiş: Thumbnail için aynı görsel
            Gender = request.Gender,
            Category = request.Category,
            DefaultBackground = request.DefaultBackground,
            DefaultLighting = request.DefaultLighting
        };

        await _modelRepository.CreateAsync(modelAsset); // CreateAsync eklenecek repo'ya
        return Ok(modelAsset);
    }
}
