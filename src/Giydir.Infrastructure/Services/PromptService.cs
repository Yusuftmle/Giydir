// src/Giydir.Infrastructure/Services/PromptService.cs
using Giydir.Core.Entities;
using Giydir.Core.Interfaces;
using System.Text.Json;

namespace Giydir.Infrastructure.Services;

public class PromptService : IPromptService
{
    public string GeneratePromptFromTemplate(Template template, string? customPrompt = null, string? background = null, string? lighting = null)
    {
        string prompt = !string.IsNullOrEmpty(customPrompt) ? customPrompt : template.PromptTemplate;

        // Eğer hiç hazır prompt yoksa (eski usul), JSON özelliklerinden üret
        if (string.IsNullOrEmpty(prompt))
        {
            return GeneratePromptFromJson(
                template.Style,
                template.Color,
                template.Pattern,
                template.Material,
                template.Category,
                background ?? template.Background,
                lighting ?? template.Lighting,
                template.Pose,
                template.CameraAngle,
                template.Mood,
                template.AdditionalAttributes
            );
        }

        // Hazır prompt'a kalite anahtar kelimelerini ekle
        var parts = new List<string> { prompt };
        
        if (!string.IsNullOrEmpty(background)) parts.Add($"{background} background");
        if (!string.IsNullOrEmpty(lighting)) parts.Add($"{lighting} lighting");

        parts.Add("Masterpiece");
        parts.Add("8k resolution");
        parts.Add("photorealistic");
        parts.Add("Vogue magazine style");
        parts.Add("high fidelity");
        parts.Add("sharp focus");

        return string.Join(", ", parts);
    }

    // YENİ: Template + Model kombinasyonu için
    public string GeneratePromptWithModelDefaults(
        Template? template,
        ModelAsset? model,
        string style = "",
        string color = "",
        string pattern = "",
        string material = "",
        string category = "upper_body",
        string? background = null,
        string? lighting = null)
    {
        // Template varsa template özelliklerini kullan (öncelikli)
        // Template yoksa model'in default özelliklerini kullan
        // Explicit parametreler her zaman en öncelikli
        
        string? finalBackground = background ?? template?.Background ?? model?.DefaultBackground;
        string? finalLighting = lighting ?? template?.Lighting ?? model?.DefaultLighting;
        string? pose = template?.Pose ?? model?.DefaultPose;
        string? cameraAngle = template?.CameraAngle ?? model?.DefaultCameraAngle;
        string? mood = template?.Mood ?? model?.DefaultMood;

        return GeneratePromptFromJson(
            style,
            color,
            pattern,
            material,
            category,
            finalBackground,
            finalLighting,
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

        // Base quality keywords - ORGANIC STYLE
        promptParts.Add("Raw photo");
        promptParts.Add("High-end editorial photography");
        promptParts.Add("natural skin texture with pores");
        promptParts.Add("soft natural lighting");
        promptParts.Add("slight film grain");
        promptParts.Add("highly detailed fabric weave");

        switch (category.ToLower())
        {
            case "upper_body":
                promptParts.Add("35mm film photo of a person wearing a designer top");
                break;
            case "lower_body":
                promptParts.Add("Full body fashion shot of a person in high-end trousers");
                break;
            case "dresses":
                promptParts.Add("Elegant portrait of a person in a luxury dress");
                break;
            default:
                promptParts.Add("Candid fashion shot");
                break;
        }

        if (!string.IsNullOrEmpty(style))
            promptParts.Add($"{style} style");

        if (!string.IsNullOrEmpty(color))
            promptParts.Add($"{color} color");

        if (!string.IsNullOrEmpty(material))
            promptParts.Add($"made of {material} material");

        if (!string.IsNullOrEmpty(pattern) && pattern.ToLower() != "solid")
            promptParts.Add($"with subtle {pattern} pattern");

        // Sahne özellikleri - Template varsa template, yoksa model default
        if (!string.IsNullOrEmpty(background))
            promptParts.Add($"shot on location at {background}");
        else
            promptParts.Add("studio setting");

        if (!string.IsNullOrEmpty(lighting))
            promptParts.Add($"{lighting}, moody natural light, realistic shadows");
        else
            promptParts.Add("soft window light");

        if (!string.IsNullOrEmpty(pose))
            promptParts.Add($"{pose} pose");

        if (!string.IsNullOrEmpty(cameraAngle))
            promptParts.Add($"{cameraAngle} camera angle");

        if (!string.IsNullOrEmpty(mood))
            promptParts.Add($"{mood} expression");

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

        promptParts.Add("hyper-detailed");
        promptParts.Add("cinematic composition");
        promptParts.Add("blurred background");

        return string.Join(", ", promptParts);
    }
}