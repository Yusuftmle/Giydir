using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeductCreditsAsync(int userId, int amount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Credits < amount)
            return false;

        user.Credits -= amount;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task AddCreditsAsync(int userId, int amount)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return;

        user.Credits += amount;
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetCreditsAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.Credits ?? 0;
    }
}




