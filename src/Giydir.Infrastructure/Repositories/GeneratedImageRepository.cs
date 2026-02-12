using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Repositories;

public class GeneratedImageRepository : IGeneratedImageRepository
{
    private readonly AppDbContext _context;

    public GeneratedImageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GeneratedImage?> GetByIdAsync(int id)
    {
        return await _context.GeneratedImages
            .Include(g => g.ModelAsset)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<GeneratedImage> CreateAsync(GeneratedImage image)
    {
        _context.GeneratedImages.Add(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task UpdateAsync(GeneratedImage image)
    {
        _context.GeneratedImages.Update(image);
        await _context.SaveChangesAsync();
    }
}

