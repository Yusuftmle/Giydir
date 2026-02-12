using Giydir.Core.DTOs;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[Route("api/[controller]")]
public class ProjectsController : BaseController
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAuthService _authService;

    public ProjectsController(IProjectRepository projectRepository, IAuthService authService)
    {
        _projectRepository = projectRepository;
        _authService = authService;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<ProjectDto>>> GetAll()
    {
        var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");
        var projects = await _projectRepository.GetByUserIdAsync(userId);

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
    [Authorize]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { error = "Proje adı gerekli" });

        var userId = UserId ?? throw new UnauthorizedAccessException("Giriş yapmalısınız");

        var project = new Project
        {
            UserId = userId,
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
