// src/Giydir.Infrastructure/Services/PromptService.cs
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using System.Text.Json;

namespace Giydir.Infrastructure.Services;

public class PromptService : IPromptService
{
    public string GeneratePromptFromTemplate(Template template, string? customPrompt = null)
    {
        if (!string.IsNullOrEmpty(customPrompt))
        {
            return customPrompt;
        }

        if (!string.IsNullOrEmpty(template.PromptTemplate))
        {
            return template.PromptTemplate;
        }

        return GeneratePromptFromJson(
            template.Style,
            template.Color,
            template.Pattern,
            template.Material,
            template.Category,
            template.Background,
            template.Lighting,
            template.Pose,
            template.CameraAngle,
            template.Mood,
            template.AdditionalAttributes
        );
    }

    // YENİ: Template + Model kombinasyonu için
    public string GeneratePromptWithModelDefaults(
        Template? template,
        ModelAsset? model,
        string style = "",
        string color = "",
        string pattern = "",
        string material = "",
        string category = "upper_body")
    {
        // Template varsa template özelliklerini kullan (öncelikli)
        // Template yoksa model'in default özelliklerini kullan
        
        string? background = template?.Background ?? model?.DefaultBackground;
        string? lighting = template?.Lighting ?? model?.DefaultLighting;
        string? pose = template?.Pose ?? model?.DefaultPose;
        string? cameraAngle = template?.CameraAngle ?? model?.DefaultCameraAngle;
        string? mood = template?.Mood ?? model?.DefaultMood;

        return GeneratePromptFromJson(
            style,
            color,
            pattern,
            material,
            category,
            background,
            lighting,
            pose,
            cameraAngle,
            mood,
            template?.AdditionalAttributes
        );
    }

    public string GeneratePromptFromJson(
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
        string? additionalAttributes = null)
    {
        var promptParts = new List<string>();

        switch (category.ToLower())
        {
            case "upper_body":
                promptParts.Add("professional");
                break;
            case "lower_body":
                promptParts.Add("fashion");
                break;
            case "dresses":
                promptParts.Add("elegant");
                break;
        }

        if (!string.IsNullOrEmpty(color))
            promptParts.Add(color);

        if (!string.IsNullOrEmpty(material))
            promptParts.Add(material);

        if (!string.IsNullOrEmpty(style))
            promptParts.Add(style);

        if (!string.IsNullOrEmpty(pattern) && pattern.ToLower() != "solid")
            promptParts.Add($"{pattern} pattern");

        switch (category.ToLower())
        {
            case "upper_body":
                promptParts.Add("shirt or top");
                break;
            case "lower_body":
                promptParts.Add("pants or trousers");
                break;
            case "dresses":
                promptParts.Add("dress");
                break;
        }

        // Sahne özellikleri - Template varsa template, yoksa model default
        if (!string.IsNullOrEmpty(background))
            promptParts.Add($"{background} background");
        else
            promptParts.Add("clean white background");

        if (!string.IsNullOrEmpty(lighting))
            promptParts.Add($"{lighting} lighting");
        else
            promptParts.Add("studio lighting");

        if (!string.IsNullOrEmpty(pose))
            promptParts.Add($"{pose} pose");

        if (!string.IsNullOrEmpty(cameraAngle))
            promptParts.Add($"{cameraAngle} angle");

        if (!string.IsNullOrEmpty(mood))
            promptParts.Add($"{mood} mood");

        if (!string.IsNullOrEmpty(additionalAttributes))
        {
            try
            {
                var attrs = JsonSerializer.Deserialize<Dictionary<string, string>>(additionalAttributes);
                if (attrs != null)
                {
                    foreach (var attr in attrs)
                    {
                        promptParts.Add(attr.Value);
                    }
                }
            }
            catch { }
        }

        promptParts.Add("high quality fashion photography");
        promptParts.Add("professional product image");

        return string.Join(", ", promptParts);
    }
}