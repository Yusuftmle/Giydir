using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Repositories;

public class SavedPromptRepository : ISavedPromptRepository
{
    private readonly AppDbContext _context;

    public SavedPromptRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SavedPrompt?> GetByIdAsync(int id)
    {
        return await _context.SavedPrompts.FindAsync(id);
    }

    public async Task<List<SavedPrompt>> GetByUserIdAsync(int userId)
    {
        return await _context.SavedPrompts
            .Where(p => p.CreatedByUserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SavedPrompt>> GetAllAsync()
    {
        return await _context.SavedPrompts
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task CreateAsync(SavedPrompt savedPrompt)
    {
        _context.SavedPrompts.Add(savedPrompt);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SavedPrompt savedPrompt)
    {
        _context.SavedPrompts.Update(savedPrompt);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var prompt = await GetByIdAsync(id);
        if (prompt != null)
        {
            _context.SavedPrompts.Remove(prompt);
            await _context.SaveChangesAsync();
        }
    }
}
