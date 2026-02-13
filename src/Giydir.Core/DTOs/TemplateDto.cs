namespace Giydir.Core.DTOs;

public class TemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string Pattern { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    
    // Sahne özellikleri
    public string? Background { get; set; }
    public string? Lighting { get; set; }
    public string? Pose { get; set; }
    public string? CameraAngle { get; set; }
    public string? Mood { get; set; }
    public bool RequiresModel { get; set; }
    
    public string? AdditionalAttributes { get; set; }
    public string PromptTemplate { get; set; } = string.Empty;
}

public class GenerateFromTemplateDto
{
    public int TemplateId { get; set; }
    public string? ModelAssetId { get; set; } // Opsiyonel: Template ile model de seçilebilir
    public string? CustomPrompt { get; set; } // Opsiyonel: kullanıcı özel prompt ekleyebilir
    public string AspectRatio { get; set; } = "4:3";
    public string OutputFormat { get; set; } = "jpg";
    public int? ProjectId { get; set; }
}

public class AIGenerationResponseDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int? ImageId { get; set; }
    public string? PredictionId { get; set; }
}

