using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IModelAssetRepository
{
    Task<List<ModelAsset>> GetAllAsync();
    Task<ModelAsset?> GetByIdAsync(string id);
}

