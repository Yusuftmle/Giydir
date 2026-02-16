namespace Giydir.Core.Interfaces;

public interface IImageDownloadService
{
    Task<string> DownloadAndSaveAsync(string imageUrl, string? fileName = null);
}




