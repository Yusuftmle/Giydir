using Giydir.Core.Interfaces;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Giydir.Infrastructure.Services;

public class ImageOptimizationService : IImageOptimizationService
{
    private readonly ILogger<ImageOptimizationService> _logger;

    public ImageOptimizationService(ILogger<ImageOptimizationService> logger)
    {
        _logger = logger;
    }

    public async Task<string> OptimizeAsync(string imagePath, int maxWidth = 2048, int quality = 90)
    {
        try
        {
            // imagePath = relative path like /uploads/generated/abc.jpg
            // We need the full path on disk
            if (imagePath.StartsWith("/"))
            {
                // Return as-is if already relative
            }

            using var image = await Image.LoadAsync(imagePath);

            var originalWidth = image.Width;
            var originalHeight = image.Height;

            // Resize only if larger than maxWidth
            if (image.Width > maxWidth)
            {
                var ratio = (double)maxWidth / image.Width;
                var newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(maxWidth, newHeight));

                _logger.LogInformation("Görsel boyutlandırıldı: {W1}x{H1} -> {W2}x{H2}",
                    originalWidth, originalHeight, maxWidth, newHeight);
            }

            // Save as optimized JPEG
            var encoder = new JpegEncoder { Quality = quality };
            var optimizedPath = Path.ChangeExtension(imagePath, ".jpg");
            await image.SaveAsync(optimizedPath, encoder);

            var fileSize = new FileInfo(optimizedPath).Length;
            _logger.LogInformation("Görsel optimize edildi: {Path}, Boyut: {Size}KB",
                optimizedPath, fileSize / 1024);

            return optimizedPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Görsel optimizasyon hatası: {Path}", imagePath);
            // Return original path if optimization fails
            return imagePath;
        }
    }
}




