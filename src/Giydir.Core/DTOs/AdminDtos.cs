using System;

namespace Giydir.Core.DTOs;

public class AdminGenerateModelRequestDto
{
    public string Prompt { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public string AspectRatio { get; set; } = "3:4";
}

public class AdminSavePromptRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string PromptText { get; set; } = string.Empty;
    public string? NegativePrompt { get; set; }
    public string? ResultImageUrl { get; set; }
    public string? SettingsJson { get; set; }
}

public class AdminPublishModelRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Gender { get; set; } = "Female";
    public string Category { get; set; } = "Studio / Casual";
    public string? DefaultBackground { get; set; }
    public string? DefaultLighting { get; set; }
}
