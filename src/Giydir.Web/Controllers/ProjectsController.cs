using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectRepository _projectRepository;

    public ProjectsController(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        // MVP: Tek kullanıcı (userId = 1)
        var projects = await _projectRepository.GetByUserIdAsync(1);

        var dtos = projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            CreatedAt = p.CreatedAt,
            ImageCount = p.Images?.Count ?? 0
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Proje adı gerekli" });

        var project = new Project
        {
            UserId = 1, // MVP: Tek kullanıcı
            Name = dto.Name
        };

        await _projectRepository.CreateAsync(project);

        return Ok(new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            CreatedAt = project.CreatedAt,
            ImageCount = 0
        });
    }
}

