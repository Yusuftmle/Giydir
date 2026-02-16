namespace Giydir.Core.Interfaces;

public interface IImageOptimizationService
{
    Task<string> OptimizeAsync(string imagePath, int maxWidth = 2048, int quality = 90);
}




