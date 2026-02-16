using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Repositories;

public class ModelAssetRepository : IModelAssetRepository
{
    private readonly AppDbContext _context;

    public ModelAssetRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task UpdateAsync(ModelAsset model)
    {
        _context.ModelAssets.Update(model);
        await _context.SaveChangesAsync();
    }

    public async Task CreateAsync(ModelAsset model)
    {
        _context.ModelAssets.Add(model);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ModelAsset>> GetAllAsync()
    {
        return await _context.ModelAssets
            .OrderBy(m => m.Gender)
            .ThenBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<ModelAsset?> GetByIdAsync(string id)
    {
        return await _context.ModelAssets.FindAsync(id);
    }

    public async Task DeleteAsync(string id)
    {
        var model = await GetByIdAsync(id);
        if (model != null)
        {
            _context.ModelAssets.Remove(model);
            await _context.SaveChangesAsync();
        }
    }
}




