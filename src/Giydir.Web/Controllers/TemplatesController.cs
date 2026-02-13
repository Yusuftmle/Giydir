using Giydir.Core.DTOs;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[Route("api/[controller]")]
public class TemplatesController : BaseController
{
    private readonly ITemplateRepository _templateRepository;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(
        ITemplateRepository templateRepository,
        ILogger<TemplatesController> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<TemplateDto>>> GetAll([FromQuery] string? category = null)
    {
        try
        {
            List<Giydir.Core.Entities.Template> templates;
            
            if (!string.IsNullOrEmpty(category))
            {
                templates = await _templateRepository.GetByCategoryAsync(category);
            }
            else
            {
                templates = await _templateRepository.GetAllAsync();
            }

            var dtos = templates.Select(t => new TemplateDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category,
                ThumbnailPath = t.ThumbnailPath,
                Style = t.Style,
                Color = t.Color,
                Pattern = t.Pattern,
                Material = t.Material,
                // Sahne özellikleri
                Background = t.Background,
                Lighting = t.Lighting,
                Pose = t.Pose,
                CameraAngle = t.CameraAngle,
                Mood = t.Mood,
                RequiresModel = t.RequiresModel,
                AdditionalAttributes = t.AdditionalAttributes,
                PromptTemplate = t.PromptTemplate
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template listesi getirme hatası");
            return StatusCode(500, new { error = "Template listesi alınırken bir hata oluştu" });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TemplateDto>> GetById(int id)
    {
        try
        {
            var template = await _templateRepository.GetByIdAsync(id);
            if (template == null || !template.IsActive)
                return NotFound(new { error = "Template bulunamadı" });

            var dto = new TemplateDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                ThumbnailPath = template.ThumbnailPath,
                Style = template.Style,
                Color = template.Color,
                Pattern = template.Pattern,
                Material = template.Material,
                // Sahne özellikleri
                Background = template.Background,
                Lighting = template.Lighting,
                Pose = template.Pose,
                CameraAngle = template.CameraAngle,
                Mood = template.Mood,
                RequiresModel = template.RequiresModel,
                AdditionalAttributes = template.AdditionalAttributes,
                PromptTemplate = template.PromptTemplate
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Template getirme hatası: {TemplateId}", id);
            return StatusCode(500, new { error = "Template alınırken bir hata oluştu" });
        }
    }
}

