using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IGeneratedImageRepository
{
    Task<GeneratedImage?> GetByIdAsync(int id);
    Task<GeneratedImage> CreateAsync(GeneratedImage image);
    Task UpdateAsync(GeneratedImage image);
}



