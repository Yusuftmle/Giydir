using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Repositories;

public class PoseRepository : IPoseRepository
{
    private readonly AppDbContext _context;

    public PoseRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Pose>> GetAllActiveAsync()
    {
        return await _context.Poses
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<List<Pose>> GetAllAsync()
    {
        return await _context.Poses
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<Pose?> GetByIdAsync(int id)
    {
        return await _context.Poses.FindAsync(id);
    }

    public async Task CreateAsync(Pose pose)
    {
        _context.Poses.Add(pose);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Pose pose)
    {
        var existing = await _context.Poses.FindAsync(pose.Id);
        if (existing != null)
        {
            existing.Name = pose.Name;
            existing.ImagePath = pose.ImagePath;
            existing.PromptKeyword = pose.PromptKeyword;
            existing.SortOrder = pose.SortOrder;
            existing.IsActive = pose.IsActive;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var pose = await _context.Poses.FindAsync(id);
        if (pose != null)
        {
            _context.Poses.Remove(pose);
            await _context.SaveChangesAsync();
        }
    }
}
