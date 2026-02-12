# AI Virtual Try-On SaaS Platform - MVP Prompt for Cursor

## Proje Özeti
Küçük butik sahipleri için AI destekli sanal giyim platformu. Kullanıcılar kendi kıyafet fotoğraflarını yükleyip, hazır AI model pozları üzerine giydirerek profesyonel ürün fotoğrafları oluşturabilecek.

## Teknoloji Stack
- **Backend**: ASP.NET Core 8 Web API
- **Frontend**: Blazor Server (daha hızlı MVP için)
- **Database**: SQLite (MVP için, sonra PostgreSQL'e geçilebilir)
- **File Storage**: Yerel dosya sistemi (MVP için, sonra Azure Blob)
- **AI Service**: Replicate API (IDM-VTON modeli)
- **Auth**: ASP.NET Core Identity (basit email/password)

## Proje Yapısı

```
NanoBanana/
├── NanoBanana.sln
├── src/
│   ├── NanoBanana.Web/              # Blazor Server + API
│   │   ├── Controllers/
│   │   ├── Pages/
│   │   ├── Components/
│   │   ├── Services/
│   │   ├── Data/
│   │   └── wwwroot/
│   ├── NanoBanana.Core/             # Domain modeller
│   │   ├── Entities/
│   │   ├── Interfaces/
│   │   └── DTOs/
│   └── NanoBanana.Infrastructure/   # Data access, external services
│       ├── Data/
│       ├── Repositories/
│       └── ExternalServices/
```

## Core Features (3 Günlük MVP)

### Gün 1: Backend + AI Entegrasyonu
1. **ASP.NET Core Web API projesi oluştur**
   - Clean Architecture prensibi
   - Minimal API veya Controller-based (tercih: Controller)

2. **Database modelleri**:
```csharp
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string PasswordHash { get; set; }
    public int Credits { get; set; } = 10; // Başlangıç kredisi
    public DateTime CreatedAt { get; set; }
}

public class Project
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GeneratedImage> Images { get; set; }
}

public class GeneratedImage
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string OriginalClothingPath { get; set; }
    public string ModelAssetId { get; set; } // Hangi model pozu kullanıldı
    public string GeneratedImagePath { get; set; }
    public string Status { get; set; } // Processing, Completed, Failed
    public DateTime CreatedAt { get; set; }
}

public class ModelAsset
{
    public string Id { get; set; } // model-1, model-2, etc.
    public string Name { get; set; } // "Casual Pose 1"
    public string ThumbnailPath { get; set; }
    public string FullImagePath { get; set; }
    public string Gender { get; set; } // Male, Female, Unisex
    public string Category { get; set; } // upper_body, lower_body, dresses
}
```

3. **Replicate Service implementasyonu**:
```csharp
public interface IVirtualTryOnService
{
    Task<string> GenerateTryOnImageAsync(string clothingImagePath, string modelAssetId);
    Task<string> CheckStatusAsync(string predictionId);
}

public class ReplicateVirtualTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiToken;
    
    public async Task<string> GenerateTryOnImageAsync(string clothingImagePath, string modelAssetId)
    {
        // Replicate API'ye istek at
        // Model: "cuuupid/idm-vton" veya benzeri
        // Input: garm_img (kıyafet), human_img (model), category
        // Return: prediction ID veya direkt image URL
    }
}
```

4. **API Endpoints**:
```
POST   /api/tryon/generate          # Yeni try-on başlat
GET    /api/tryon/status/{id}       # İşlem durumu
GET    /api/models                  # Mevcut model pozları listesi
POST   /api/upload/clothing         # Kıyafet fotoğrafı yükle
GET    /api/projects                # Kullanıcı projeleri
POST   /api/projects                # Yeni proje oluştur
```

### Gün 2: Blazor Frontend
1. **Blazor Server sayfaları**:

**Pages/Index.razor** - Ana sayfa
```razor
@page "/"
<h1>NanoBanana - AI Virtual Try-On</h1>
<p>Kıyafetlerinizi profesyonel modeller üzerinde gösterin!</p>
<a href="/editor" class="btn btn-primary">Hemen Başla</a>
```

**Pages/Editor.razor** - Ana editör sayfası
```razor
@page "/editor"
@inject IVirtualTryOnService TryOnService

<div class="container">
    <div class="row">
        <!-- Sol: Kıyafet Yükleme -->
        <div class="col-md-4">
            <h3>1. Kıyafetinizi Yükleyin</h3>
            <InputFile OnChange="HandleClothingUpload" />
            @if (!string.IsNullOrEmpty(uploadedClothingUrl))
            {
                <img src="@uploadedClothingUrl" class="img-thumbnail" />
            }
        </div>
        
        <!-- Orta: Model Seçimi -->
        <div class="col-md-4">
            <h3>2. Model Pozu Seçin</h3>
            <ModelSelector @bind-SelectedModelId="selectedModelId" />
        </div>
        
        <!-- Sağ: Sonuç -->
        <div class="col-md-4">
            <h3>3. Sonuç</h3>
            @if (isProcessing)
            {
                <div class="spinner-border"></div>
                <p>AI görselinizi oluşturuyor...</p>
            }
            else if (!string.IsNullOrEmpty(resultImageUrl))
            {
                <img src="@resultImageUrl" class="img-fluid" />
                <button @onclick="DownloadImage" class="btn btn-success">İndir</button>
            }
        </div>
    </div>
    
    <button @onclick="GenerateImage" class="btn btn-primary mt-3" disabled="@(!CanGenerate)">
        Oluştur
    </button>
</div>

@code {
    private string uploadedClothingUrl;
    private string selectedModelId;
    private string resultImageUrl;
    private bool isProcessing;
    
    private bool CanGenerate => !string.IsNullOrEmpty(uploadedClothingUrl) 
                                && !string.IsNullOrEmpty(selectedModelId) 
                                && !isProcessing;
    
    private async Task HandleClothingUpload(InputFileChangeEventArgs e)
    {
        // Dosyayı sunucuya yükle
        // uploadedClothingUrl'i set et
    }
    
    private async Task GenerateImage()
    {
        isProcessing = true;
        try
        {
            resultImageUrl = await TryOnService.GenerateTryOnImageAsync(
                uploadedClothingUrl, 
                selectedModelId
            );
        }
        finally
        {
            isProcessing = false;
        }
    }
}
```

**Components/ModelSelector.razor** - Model pozu seçici
```razor
<div class="model-grid">
    @foreach (var model in models)
    {
        <div class="model-card @(model.Id == SelectedModelId ? "selected" : "")" 
             @onclick="() => SelectModel(model.Id)">
            <img src="@model.ThumbnailPath" />
            <p>@model.Name</p>
        </div>
    }
</div>

@code {
    [Parameter] public string SelectedModelId { get; set; }
    [Parameter] public EventCallback<string> SelectedModelIdChanged { get; set; }
    
    private List<ModelAsset> models = new();
    
    protected override async Task OnInitializedAsync()
    {
        // API'den model listesini çek
        models = await Http.GetFromJsonAsync<List<ModelAsset>>("/api/models");
    }
    
    private async Task SelectModel(string modelId)
    {
        SelectedModelId = modelId;
        await SelectedModelIdChanged.InvokeAsync(modelId);
    }
}
```

2. **CSS styling** (wwwroot/css/site.css):
```css
.model-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    gap: 1rem;
}

.model-card {
    border: 2px solid #ddd;
    border-radius: 8px;
    padding: 0.5rem;
    cursor: pointer;
    transition: all 0.2s;
}

.model-card:hover {
    border-color: #007bff;
    transform: scale(1.05);
}

.model-card.selected {
    border-color: #28a745;
    background-color: #e8f5e9;
}
```

### Gün 3: Polish + Test
1. **Hata yönetimi**:
   - Try-catch blokları
   - Kullanıcıya anlamlı hata mesajları
   - Logging (Serilog ekle)

2. **Loading states**:
   - Spinner komponentleri
   - Progress bar (opsiyonel)

3. **Image optimization**:
   - Yüklenen görselleri resize et (max 1024px)
   - Format dönüşümü (hepsi JPEG)

4. **Basit auth** (opsiyonel MVP için):
   - Login/Register sayfası
   - Session yönetimi
   - Credits sistemi

5. **Seed data**:
   - 5-10 adet örnek model pozu ekle (wwwroot/assets/models/)
   - Varsayılan bir kullanıcı oluştur

## Önemli Notlar

### Replicate API Kullanımı
```bash
# appsettings.json
{
  "Replicate": {
    "ApiToken": "YOUR_REPLICATE_API_TOKEN",
    "ModelVersion": "cuuupid/idm-vton:c871bb9b046607b680449ecbae55fd8c6d945e0a1948644bf2361b3d021d3ff4"
  }
}
```

### File Upload Handling
```csharp
[HttpPost("upload/clothing")]
public async Task<IActionResult> UploadClothing(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest("Dosya yüklenmedi");
        
    // Dosya tipi kontrolü
    var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
    if (!allowedTypes.Contains(file.ContentType))
        return BadRequest("Sadece JPEG, PNG, WEBP destekleniyor");
    
    // Benzersiz dosya adı oluştur
    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
    var filePath = Path.Combine("wwwroot/uploads", fileName);
    
    // Kaydet
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    
    return Ok(new { url = $"/uploads/{fileName}" });
}
```

### Replicate API Integration (Detaylı)
```csharp
public class ReplicateVirtualTryOnService : IVirtualTryOnService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    
    public ReplicateVirtualTryOnService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Token", 
                _config["Replicate:ApiToken"]
            );
    }
    
    public async Task<string> GenerateTryOnImageAsync(string clothingImagePath, string modelAssetId)
    {
        // Model asset'inden tam URL'yi al
        var modelImageUrl = GetModelImageUrl(modelAssetId);
        var clothingImageUrl = GetFullUrl(clothingImagePath);
        
        var payload = new
        {
            version = _config["Replicate:ModelVersion"],
            input = new
            {
                garm_img = clothingImageUrl,      // Kıyafet görseli
                human_img = modelImageUrl,         // Model pozu
                garment_des = "clothing",          // Açıklama
                category = "upper_body"            // veya lower_body, dresses
            }
        };
        
        var response = await _httpClient.PostAsJsonAsync(
            "https://api.replicate.com/v1/predictions", 
            payload
        );
        
        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();
        
        // Async işlem - prediction ID'yi döndür
        return result.Id;
    }
    
    public async Task<string> CheckStatusAsync(string predictionId)
    {
        var response = await _httpClient.GetAsync(
            $"https://api.replicate.com/v1/predictions/{predictionId}"
        );
        
        var result = await response.Content.ReadFromJsonAsync<ReplicatePredictionResponse>();
        
        if (result.Status == "succeeded")
        {
            // Output URL'yi döndür
            return result.Output?.FirstOrDefault();
        }
        else if (result.Status == "failed")
        {
            throw new Exception("AI görsel oluşturma başarısız");
        }
        
        // Hala processing
        return null;
    }
    
    private string GetFullUrl(string relativePath)
    {
        // Yerel geliştirmede: http://localhost:5000/uploads/abc.jpg
        // Production'da: tam URL
        return $"{_config["BaseUrl"]}{relativePath}";
    }
}

public class ReplicatePredictionResponse
{
    public string Id { get; set; }
    public string Status { get; set; } // starting, processing, succeeded, failed
    public List<string> Output { get; set; }
    public string Error { get; set; }
}
```

## Cursor'a Özel Komutlar

### Proje Oluşturma
```bash
# Terminal'de çalıştır
dotnet new sln -n NanoBanana
dotnet new web -n NanoBanana.Web -o src/NanoBanana.Web
dotnet new classlib -n NanoBanana.Core -o src/NanoBanana.Core
dotnet new classlib -n NanoBanana.Infrastructure -o src/NanoBanana.Infrastructure

dotnet sln add src/NanoBanana.Web
dotnet sln add src/NanoBanana.Core
dotnet sln add src/NanoBanana.Infrastructure

# Referansları ekle
cd src/NanoBanana.Web
dotnet add reference ../NanoBanana.Core
dotnet add reference ../NanoBanana.Infrastructure

cd ../NanoBanana.Infrastructure
dotnet add reference ../NanoBanana.Core
```

### Gerekli NuGet Paketleri
```bash
# NanoBanana.Web için
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Serilog.AspNetCore

# NanoBanana.Infrastructure için
dotnet add package Microsoft.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
```

## Örnek Model Assets Hazırlama

MVP için 5-10 model pozu hazırla:
1. **Midjourney/Stable Diffusion ile oluştur**:
   - Prompt: "full body photo of mannequin wearing plain white t-shirt, neutral background, professional photography, front view, arms at sides"
   - Farklı pozlar: front view, side view, casual pose
   - Erkek ve kadın modeller

2. **Veya ücretsiz stock fotoğraf kullan**:
   - Unsplash, Pexels'den model fotoğrafları
   - Lisansa dikkat et

3. **Dosya yapısı**:
```
wwwroot/
  assets/
    models/
      male-casual-1.jpg
      male-casual-2.jpg
      female-casual-1.jpg
      female-professional-1.jpg
```

## Testing Checklist

### Manuel Test Senaryoları
- [ ] Kullanıcı t-shirt fotoğrafı yükleyebiliyor
- [ ] Model pozu seçimi çalışıyor
- [ ] "Oluştur" butonu beklendiği gibi çalışıyor
- [ ] AI görsel 30-60 saniye içinde geliyor
- [ ] Sonuç görseli indirilebiliyor
- [ ] Hata durumunda uygun mesaj gösteriliyor
- [ ] Birden fazla görsel üretebiliyor (credits varsa)

## Deployment (Opsiyonel - Gün 3 sonrası)

### Hızlı Deploy Seçenekleri
1. **Azure App Service** (ücretsiz tier)
2. **Railway.app** (kolay deployment)
3. **Heroku** (alternative)

### Environment Variables
```
REPLICATE_API_TOKEN=r8_xxx
DATABASE_CONNECTION=Data Source=app.db
BASE_URL=https://yourapp.com
```

## Önemli: MVP Sınırları

Bu MVP'de YOKTUR:
- ❌ Ödeme sistemi (şimdilik credits manuel)
- ❌ Çoklu kullanıcı yönetimi (tek kullanıcı test için yeterli)
- ❌ Email doğrulama
- ❌ Gelişmiş görsel düzenleme
- ❌ Toplu işlem (batch processing)
- ❌ API rate limiting
- ❌ Comprehensive error logging

MVP'de OLMASI GEREKENLER:
- ✅ Kıyafet yükleme
- ✅ Model seçimi
- ✅ AI görsel üretimi
- ✅ Sonuç indirme
- ✅ Basit auth (opsiyonel)
- ✅ Temel hata yönetimi

## Son Notlar

1. **Replicate API Token** almayı unutma: https://replicate.com
2. **IDM-VTON modeli** ücretsiz değil - pricing kontrolü yap
3. **Alternative modeller**: 
   - OOTDiffusion
   - Kolors Virtual Try-On
4. **Seed data** için hazır görseller kullan, zaman kaybetme

## Cursor'a Ver ve Başla!

Bu prompt'u Cursor'a ver ve şunu söyle:
> "Bu prompt'a göre 3 günde MVP çıkarmam lazım. Önce proje yapısını oluştur, sonra Gün 1 task'lerini implement et. Her adımı açıklayarak ilerle ve çalışan kod üret."

İyi şanlar! 🚀
