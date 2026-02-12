using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IProjectRepository
{
    Task<List<Project>> GetByUserIdAsync(int userId);
    Task<Project?> GetByIdAsync(int id);
    Task<Project> CreateAsync(Project project);
}


