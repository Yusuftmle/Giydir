using Giydir.Core.DTOs;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ModelsController : ControllerBase
{
    private readonly IModelAssetRepository _modelAssetRepository;

    public ModelsController(IModelAssetRepository modelAssetRepository)
    {
        _modelAssetRepository = modelAssetRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ModelAssetDto>>> GetAll()
    {
        var models = await _modelAssetRepository.GetAllAsync();

        var dtos = models.Select(m => new ModelAssetDto
        {
            Id = m.Id,
            Name = m.Name,
            ThumbnailPath = m.ThumbnailPath,
            Gender = m.Gender,
            Category = m.Category
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ModelAssetDto>> GetById(string id)
    {
        var model = await _modelAssetRepository.GetByIdAsync(id);
        if (model == null)
            return NotFound();

        return Ok(new ModelAssetDto
        {
            Id = model.Id,
            Name = model.Name,
            ThumbnailPath = model.ThumbnailPath,
            Gender = model.Gender,
            Category = model.Category
        });
    }
}

