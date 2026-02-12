using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<bool> DeductCreditsAsync(int userId, int amount);
    Task AddCreditsAsync(int userId, int amount);
    Task<int> GetCreditsAsync(int userId);
}


