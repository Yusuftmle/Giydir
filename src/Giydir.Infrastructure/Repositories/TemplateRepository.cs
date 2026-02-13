using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly AppDbContext _context;

    public TemplateRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Template>> GetAllAsync()
    {
        return await _context.Templates
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<List<Template>> GetByCategoryAsync(string category)
    {
        return await _context.Templates
            .Where(t => t.IsActive && t.Category == category)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Template?> GetByIdAsync(int id)
    {
        return await _context.Templates.FindAsync(id);
    }

    public async Task<Template> CreateAsync(Template template)
    {
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task UpdateAsync(Template template)
    {
        _context.Templates.Update(template);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var template = await _context.Templates.FindAsync(id);
        if (template != null)
        {
            template.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }
}



