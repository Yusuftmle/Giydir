using System.Collections.Generic;
using System.Threading.Tasks;
using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface ISavedPromptRepository
{
    Task<SavedPrompt?> GetByIdAsync(int id);
    Task<List<SavedPrompt>> GetByUserIdAsync(int userId);
    Task<List<SavedPrompt>> GetAllAsync();
    Task CreateAsync(SavedPrompt savedPrompt);
    Task UpdateAsync(SavedPrompt savedPrompt);
    Task DeleteAsync(int id);
}
