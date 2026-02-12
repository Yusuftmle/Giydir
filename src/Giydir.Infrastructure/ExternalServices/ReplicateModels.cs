namespace Giydir.Infrastructure.ExternalServices;

internal class ReplicatePredictionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // starting, processing, succeeded, failed, canceled
    public List<string>? Output { get; set; }
    public string? Error { get; set; }
    public ReplicatePredictionUrls? Urls { get; set; }
}

internal class ReplicatePredictionUrls
{
    public string? Stream { get; set; }
    public string? Get { get; set; }
    public string? Cancel { get; set; }
    public string? Web { get; set; }
}

