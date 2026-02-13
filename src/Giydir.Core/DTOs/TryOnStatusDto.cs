namespace Giydir.Core.DTOs;

public class TryOnStatusDto
{
    public int ImageId { get; set; }
    public string Status { get; set; } = string.Empty; // Processing, Completed, Failed
    public string? GeneratedImageUrl { get; set; }
    public string? ErrorMessage { get; set; }
}



