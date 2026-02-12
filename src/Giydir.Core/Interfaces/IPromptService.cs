using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IPromptService
{
    /// <summary>
    /// Template'den prompt oluşturur
    /// </summary>
    string GeneratePromptFromTemplate(Template template, string? customPrompt = null);
    
    /// <summary>
    /// JSON verilerinden prompt oluşturur
    /// </summary>
    string GeneratePromptFromJson(string style, string color, string pattern, string material, string category, string? additionalAttributes = null);
}

