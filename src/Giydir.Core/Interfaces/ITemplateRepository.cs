using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface ITemplateRepository
{
    Task<List<Template>> GetAllAsync();
    Task<List<Template>> GetByCategoryAsync(string category);
    Task<Template?> GetByIdAsync(int id);
    Task<Template> CreateAsync(Template template);
    Task UpdateAsync(Template template);
    Task DeleteAsync(int id);
}

