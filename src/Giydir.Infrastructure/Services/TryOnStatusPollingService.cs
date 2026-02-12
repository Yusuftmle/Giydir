using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Giydir.Infrastructure.Services;

public class TryOnStatusPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TryOnStatusPollingService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    public TryOnStatusPollingService(
        IServiceScopeFactory scopeFactory,
        ILogger<TryOnStatusPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TryOn Status Polling Service başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollPendingImagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Status polling sırasında hata oluştu");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("TryOn Status Polling Service durduruldu.");
    }

    private async Task PollPendingImagesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tryOnService = scope.ServiceProvider.GetRequiredService<IVirtualTryOnService>();
        var imageDownloadService = scope.ServiceProvider.GetRequiredService<IImageDownloadService>();

        var pendingImages = await context.GeneratedImages
            .Where(i => i.Status == "Processing" && !string.IsNullOrEmpty(i.ReplicatePredictionId))
            .Take(10) // Max 10 at a time
            .ToListAsync(ct);

        if (pendingImages.Count == 0)
            return;

        _logger.LogInformation("Processing durumunda {Count} görsel kontrol ediliyor...", pendingImages.Count);

        foreach (var image in pendingImages)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var status = await tryOnService.CheckStatusAsync(image.ReplicatePredictionId!);

                if (status.Status == "succeeded" && !string.IsNullOrEmpty(status.OutputUrl))
                {
                    // Download and save image locally
                    var localPath = await imageDownloadService.DownloadAndSaveAsync(
                        status.OutputUrl,
                        $"gen_{image.Id}_{Guid.NewGuid():N}.jpg");

                    image.Status = "Completed";
                    image.GeneratedImagePath = localPath;

                    _logger.LogInformation("Görsel tamamlandı: ImageId={ImageId}, Path={Path}",
                        image.Id, localPath);
                }
                else if (status.Status == "failed")
                {
                    image.Status = "Failed";
                    image.ErrorMessage = status.Error ?? "AI görsel oluşturma başarısız oldu";

                    _logger.LogWarning("Görsel başarısız: ImageId={ImageId}, Error={Error}",
                        image.Id, image.ErrorMessage);
                }
                // else: still processing, do nothing
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Görsel status kontrolü hatası: ImageId={ImageId}", image.Id);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}

