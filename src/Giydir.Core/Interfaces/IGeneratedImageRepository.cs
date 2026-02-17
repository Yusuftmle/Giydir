using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;


public interface IGeneratedImageRepository
{
    Task<GeneratedImage?> GetByIdAsync(int id);
    Task<List<GeneratedImage>> GetByProjectIdAsync(int projectId);
    Task<GeneratedImage> CreateAsync(GeneratedImage image);
    Task UpdateAsync(GeneratedImage image);

}