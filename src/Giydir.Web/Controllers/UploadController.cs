using Giydir.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Giydir.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadController> _logger;

    private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB

    public UploadController(IWebHostEnvironment env, ILogger<UploadController> logger)
    {
        _env = env;
        _logger = logger;
    }

    [HttpPost("clothing")]
    public async Task<ActionResult<UploadResponseDto>> UploadClothing(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Dosya yüklenmedi" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { error = "Dosya boyutu 10MB'dan büyük olamaz" });

        if (!AllowedTypes.Contains(file.ContentType))
            return BadRequest(new { error = "Sadece JPEG, PNG ve WebP dosyaları destekleniyor" });

        try
        {
            // Upload klasörünü oluştur
            var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            // Benzersiz dosya adı oluştur
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Dosyayı kaydet
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            _logger.LogInformation("Kıyafet fotoğrafı yüklendi: {FileName}, Boyut: {Size}KB",
                fileName, file.Length / 1024);

            return Ok(new UploadResponseDto
            {
                Url = $"/uploads/{fileName}",
                FileName = fileName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dosya yükleme hatası");
            return StatusCode(500, new { error = "Dosya yüklenirken bir hata oluştu" });
        }
    }
}

