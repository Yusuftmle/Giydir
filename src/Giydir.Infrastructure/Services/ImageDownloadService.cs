using Giydir.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.Services;

public class ImageDownloadService : IImageDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ImageDownloadService> _logger;

    public ImageDownloadService(
        HttpClient httpClient,
        IWebHostEnvironment env,
        ILogger<ImageDownloadService> logger)
    {
        _httpClient = httpClient;
        _env = env;
        _logger = logger;
    }

    public async Task<string> DownloadAndSaveAsync(string imageUrl, string? fileName = null)
    {
        fileName ??= $"{Guid.NewGuid()}.jpg";

        var generatedDir = Path.Combine(_env.WebRootPath, "uploads", "generated");
        Directory.CreateDirectory(generatedDir);

        var filePath = Path.Combine(generatedDir, fileName);

        try
        {
            _logger.LogInformation("Görsel indiriliyor: {Url}", imageUrl);

            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();

            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(filePath, imageBytes);

            var relativePath = $"/uploads/generated/{fileName}";
            _logger.LogInformation("Görsel kaydedildi: {Path}, Boyut: {Size}KB",
                relativePath, imageBytes.Length / 1024);

            return relativePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görsel indirme hatası: {Url}", imageUrl);
            throw;
        }
    }
}

