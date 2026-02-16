// src/Giydir.Core/Interfaces/IPromptService.cs
using Giydir.Core.Entities;

namespace Giydir.Core.Interfaces;

public interface IPromptService
{
    string GeneratePromptFromTemplate(Template template, string? customPrompt = null, string? background = null, string? lighting = null);
    
    // YENİ
    string GeneratePromptWithModelDefaults(
        Template? template,
        ModelAsset? model,
        string style = "",
        string color = "",
        string pattern = "",
        string material = "",
        string category = "upper_body",
        string? background = null,
        string? lighting = null);
    
    string GeneratePromptFromJson(
        string style, 
        string color, 
        string pattern, 
        string material, 
        string category, 
        string? background = null,
        string? lighting = null,
        string? pose = null,
        string? cameraAngle = null,
        string? mood = null,
        string? additionalAttributes = null);
}