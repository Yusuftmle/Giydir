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

        // 1. Subject & Action (Essential)
        string subject = category.ToLower() switch
        {
            "dresses" => "A high-fashion model wearing an elegant dress",
            "lower_body" => "A fashion model wearing stylish trousers",
            _ => "A fashion model wearing a designer top"
        };
        
        if (!string.IsNullOrEmpty(pose)) subject += $" posing in {pose}";
        promptParts.Add(subject);

        // 2. Clothing Details (Garment Reference) - Critical for NanoBanana Pro
        promptParts.Add("wearing the exact garment shown in the reference image");
        
         if (!string.IsNullOrEmpty(color)) promptParts.Add($"{color} color");
         if (!string.IsNullOrEmpty(material)) promptParts.Add($"{material} fabric");
         if (!string.IsNullOrEmpty(pattern) && pattern.ToLower() != "solid") promptParts.Add($"{pattern} pattern");

        // 3. Environment & Lighting
        if (!string.IsNullOrEmpty(background)) 
            promptParts.Add($"standing in {background}");
        else 
            promptParts.Add("standing in a professional studio background");

        if (!string.IsNullOrEmpty(lighting))
            promptParts.Add($"{lighting}, soft cinematic lighting");
        else 
            promptParts.Add("soft natural lighting");

        // 4. Style & Mood
        if (!string.IsNullOrEmpty(mood)) promptParts.Add($"{mood} expression");
        if (!string.IsNullOrEmpty(style)) promptParts.Add($"{style} aesthetic");

        // 5. Quality Boosters (User Requested)
        promptParts.Add("The garment must match color, texture, and fit realistically");
        promptParts.Add("Photorealistic");
        promptParts.Add("8k resolution");
        promptParts.Add("highly detailed fabric folds");
        promptParts.Add("realistic shadows");
        promptParts.Add("depth of field");
        promptParts.Add("masterpiece");

        return string.Join(". ", promptParts);
    }
}