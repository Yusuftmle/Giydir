namespace Giydir.Core.DTOs;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ImageCount { get; set; }
}

public class CreateProjectDto
{
    public string Name { get; set; } = string.Empty;
}


