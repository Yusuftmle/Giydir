using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IPoseRepository
{
    Task<List<Pose>> GetAllActiveAsync();
    Task<List<Pose>> GetAllAsync();
    Task<Pose?> GetByIdAsync(int id);
    Task CreateAsync(Pose pose);
    Task UpdateAsync(Pose pose);
    Task DeleteAsync(int id);
}
