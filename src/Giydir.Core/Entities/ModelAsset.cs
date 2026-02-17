namespace Giydir.Core.Entities;

public class ModelAsset
{
    public string Id { get; set; } = string.Empty; // model-1, model-2, etc.
    public string Name { get; set; } = string.Empty; // "Casual Pose 1"
    public string ThumbnailPath { get; set; } = string.Empty;
    public string FullImagePath { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty; // Male, Female, Unisex
    public string Category { get; set; } = string.Empty; // upper_body, lower_body, dresses
     public string? DefaultBackground { get; set; } // "white", "studio", "outdoor", etc.
    public string? DefaultLighting { get; set; } // "studio", "natural", "soft", etc.
    public string? DefaultPose { get; set; } // "standing", "casual", etc.
    public string? DefaultCameraAngle { get; set; } // "front", "side", etc.
    public string? DefaultMood { get; set; } // "professional", "casual", etc.
    public string TriggerWord { get; set; } = "woman"; // Token for the model (e.g. "woman", "man", "ohwx")

    public List<GeneratedImage> GeneratedImages { get; set; } = new();
}
    