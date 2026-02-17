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
        var existing = await _context.ModelAssets.FindAsync(model.Id);
        if (existing != null)
        {
            existing.Name = model.Name;
            existing.ThumbnailPath = model.ThumbnailPath;
            existing.FullImagePath = model.FullImagePath;
            existing.Gender = model.Gender;
            existing.Category = model.Category;
            existing.DefaultBackground = model.DefaultBackground;
            existing.DefaultLighting = model.DefaultLighting;
            existing.DefaultPose = model.DefaultPose;
            existing.DefaultCameraAngle = model.DefaultCameraAngle;
            existing.DefaultMood = model.DefaultMood;
            existing.TriggerWord = model.TriggerWord;
            await _context.SaveChangesAsync();
        }
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
        var model = await _context.ModelAssets
            .Include(m => m.GeneratedImages)
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (model != null)
        {
            if (model.GeneratedImages.Any())
            {
                _context.GeneratedImages.RemoveRange(model.GeneratedImages);
            }
            
            _context.ModelAssets.Remove(model);
            await _context.SaveChangesAsync();
        }
    }
}
