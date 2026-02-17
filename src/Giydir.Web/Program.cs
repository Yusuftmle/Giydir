using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Giydir.Infrastructure.ExternalServices;
using Giydir.Infrastructure.Repositories;
using Giydir.Infrastructure.Services;
using Giydir.Web.Components;
using Giydir.Web.Infrastructure;
using Giydir.Web.Middleware;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/giydir-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Database - SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session & Cookie Authentication
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".Giydir.Session";
});
builder.Services.AddHttpContextAccessor(); // Already added, keeping for clarity

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IModelAssetRepository, ModelAssetRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IGeneratedImageRepository, GeneratedImageRepository>();
builder.Services.AddScoped<ITemplateRepository, TemplateRepository>();
builder.Services.AddScoped<ISavedPromptRepository, SavedPromptRepository>();
builder.Services.AddScoped<IPoseRepository, PoseRepository>();

// Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IImageOptimizationService, ImageOptimizationService>();
builder.Services.AddScoped<IPromptService, PromptService>();

// JWT Authentication
builder.Services.ConfigureAuth(builder.Configuration);

// External Services
// builder.Services.AddHttpClient<IVirtualTryOnService, ReplicateVirtualTryOnService>(); // Removed legacy VTON
builder.Services.AddHttpClient<IAIImageGenerationService, NanoBananaService>();
builder.Services.AddHttpClient<IImageDownloadService, ImageDownloadService>();

// NanoBanana Unified Engine Services
builder.Services.AddScoped<IFashionPromptBuilder, FashionPromptBuilder>();
builder.Services.AddScoped<RenderOrchestrator>();

// Background Services
builder.Services.AddHostedService<TryOnStatusPollingService>();

// HttpClient for Blazor pages (internal API calls) - with JWT token handler
builder.Services.AddScoped<AuthTokenHandler>();

builder.Services.AddScoped(sp =>
{
    var navigationManager = sp.GetRequiredService<NavigationManager>();
    var tokenHandler = sp.GetRequiredService<AuthTokenHandler>();
    
    // Inner handler'ı ayarla (HttpClientHandler)
    tokenHandler.InnerHandler = new HttpClientHandler();
    
    var httpClient = new HttpClient(tokenHandler);
    httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
    return httpClient;
});

// Authentication State Provider
builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthStateProvider>());

// Blazor Server
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// API Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    
            // Yeni profil sütunlarını ekle (EnsureCreated mevcut tablolara yeni sütun eklemez)
            try
            {
                var conn = db.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                
                // Sütun var mı kontrol et, yoksa ekle
                var columns = new[] { "Name", "Title", "BoutiqueName", "Sector", "WebsiteUrl", "Role" };
                foreach (var col in columns)
                {
                    try
                    {
                        cmd.CommandText = $"ALTER TABLE Users ADD COLUMN {col} TEXT NULL";
                        await cmd.ExecuteNonQueryAsync();
                        Log.Information("Yeni sütun eklendi: Users.{Column}", col);
                    }
                    catch (Exception)
                    {
                        // Sütun zaten varsa hata verir, normal
                    }
                }

                // Varsayılan User rolünü ata
                cmd.CommandText = "UPDATE Users SET Role = 'User' WHERE Role IS NULL";
                await cmd.ExecuteNonQueryAsync();

                // ModelAssets tablosuna TriggerWord sütunu ekle
                try
                {
                    cmd.CommandText = "ALTER TABLE ModelAssets ADD COLUMN TriggerWord TEXT DEFAULT 'woman'";
                    await cmd.ExecuteNonQueryAsync();
                    Log.Information("Yeni sütun eklendi: ModelAssets.TriggerWord");
                }
                catch (Exception)
                {
                    // Sütun zaten varsa hata verir, yut
                }

                // ModelAssets tablosuna TriggerWord sütunu ekle
                try
                {
                    cmd.CommandText = "ALTER TABLE ModelAssets ADD COLUMN TriggerWord TEXT DEFAULT 'woman'";
                    await cmd.ExecuteNonQueryAsync();
                    Log.Information("Yeni sütun eklendi: ModelAssets.TriggerWord");
                }
                catch (Exception)
                {
                    // Sütun zaten varsa hata verir, yut
                }

                // Admin kullanıcısını kontrol et/ekle
                cmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Email = 'admin@giydir.ai'";
                var adminExists = (long)(await cmd.ExecuteScalarAsync() ?? 0) > 0;
                
                if (!adminExists)
                {
                    var adminPassHash = BCrypt.Net.BCrypt.HashPassword("AdminGiydir2024!");
                    cmd.CommandText = $@"
                        INSERT INTO Users (Email, PasswordHash, Credits, Role, Name, CreatedAt) 
                        VALUES ('admin@giydir.ai', '{adminPassHash}', 9999, 'Admin', 'Giydir Admin', '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}')";
                    await cmd.ExecuteNonQueryAsync();
                    Log.Information("Admin kullanıcısı oluşturuldu: admin@giydir.ai");
                }
                else
                {
                    // Varsa rolünü Admin olarak zorla (403 hatalarını önlemek için)
                    cmd.CommandText = "UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@giydir.ai'";
                    await cmd.ExecuteNonQueryAsync();
                    Log.Information("Mevcut admin kullanıcısı rolü Admin olarak güncellendi.");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Veritabanı Users tablosu güncelleme hatası");
            }
            
            // Hardcoded modelleri temizle
            try
            {
                var conn = db.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                // Bağımlı kayıtları (GeneratedImages) temizle
                cmd.CommandText = "DELETE FROM GeneratedImages WHERE ModelAssetId IN ('elena-s', 'marcus-j', 'sophia-l', 'chloe-r', 'david-k', 'maya-t', 'zara-b', 'lily-a', 'ai-generated')";
                await cmd.ExecuteNonQueryAsync();

                // Modelleri temizle
                cmd.CommandText = "DELETE FROM ModelAssets WHERE Id IN ('elena-s', 'marcus-j', 'sophia-l', 'chloe-r', 'david-k', 'maya-t', 'zara-b', 'lily-a', 'ai-generated')";
                var deletedCount = await cmd.ExecuteNonQueryAsync();
                if (deletedCount > 0) Log.Information("{Count} adet hardcoded model ve bağlı resimleri temizlendi.", deletedCount);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Hardcoded modeller temizlenirken hata oluştu");
            }

            // SavedPrompts tablosunu oluştur
            try
            {
                var conn = db.Database.GetDbConnection();
                await conn.OpenAsync();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SavedPrompts (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        PromptText TEXT NOT NULL,
                        NegativePrompt TEXT,
                        ResultImageUrl TEXT,
                        SettingsJson TEXT,
                        CreatedAt TEXT NOT NULL,
                        CreatedByUserId INTEGER NOT NULL,
                        IsFavorite INTEGER NOT NULL DEFAULT 0,
                        PublishedModelId TEXT
                    );";
                await cmd.ExecuteNonQueryAsync();
                Log.Information("SavedPrompts tablosu kontrol edildi/oluşturuldu.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "SavedPrompts tablosu oluşturulamadı");
            }
        
        // Templates tablosunu oluştur (eğer yoksa)
        try
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            
            // Templates tablosu var mı kontrol et
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Templates'";
            var tableExists = await cmd.ExecuteScalarAsync();
            
            if (tableExists == null)
            {
                cmd.CommandText = @"
                    CREATE TABLE Templates (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Description TEXT,
                        Category TEXT NOT NULL,
                        ThumbnailPath TEXT NOT NULL,
                        Style TEXT,
                        Color TEXT,
                        Pattern TEXT,
                        Material TEXT,
                        AdditionalAttributes TEXT,
                        PromptTemplate TEXT,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL
                    );";
                await cmd.ExecuteNonQueryAsync();
                Log.Information("Templates tablosu oluşturuldu.");
                
                // Seed data ekle (Entity Framework ile)
                var templateRepo = scope.ServiceProvider.GetRequiredService<ITemplateRepository>();
                var seedTemplates = new[]
                {
                    new Giydir.Core.Entities.Template
                    {
                        Id = 1,
                        Name = "Klasik Beyaz Gömlek",
                        Description = "Profesyonel ve şık klasik beyaz gömlek",
                        Category = "upper_body",
                        ThumbnailPath = "/images/templates/white-shirt.jpg",
                        Style = "classic",
                        Color = "white",
                        Pattern = "solid",
                        Material = "cotton",
                        PromptTemplate = "professional white cotton classic shirt, clean white background, studio lighting, high quality fashion photography",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Giydir.Core.Entities.Template
                    {
                        Id = 2,
                        Name = "Spor Tişört",
                        Description = "Rahat ve modern spor tişört",
                        Category = "upper_body",
                        ThumbnailPath = "/images/templates/sport-tshirt.jpg",
                        Style = "sporty",
                        Color = "black",
                        Pattern = "solid",
                        Material = "polyester",
                        PromptTemplate = "modern black sporty t-shirt, athletic wear, clean background, professional product photography",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Giydir.Core.Entities.Template
                    {
                        Id = 3,
                        Name = "Elegant Elbise",
                        Description = "Zarif ve şık kadın elbisesi",
                        Category = "dresses",
                        ThumbnailPath = "/images/templates/elegant-dress.jpg",
                        Style = "elegant",
                        Color = "navy",
                        Pattern = "solid",
                        Material = "silk",
                        PromptTemplate = "elegant navy blue silk dress, sophisticated fashion, clean background, luxury fashion photography",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new Giydir.Core.Entities.Template
                    {
                        Id = 4,
                        Name = "Kot Pantolon",
                        Description = "Klasik mavi kot pantolon",
                        Category = "lower_body",
                        ThumbnailPath = "/images/templates/jeans.jpg",
                        Style = "casual",
                        Color = "blue",
                        Pattern = "denim",
                        Material = "denim",
                        PromptTemplate = "classic blue denim jeans, casual wear, clean background, professional fashion photography",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };
                
                foreach (var template in seedTemplates)
                {
                    try
                    {
                        // Template zaten var mı kontrol et
                        var existing = await templateRepo.GetByIdAsync(template.Id);
                        if (existing == null)
                        {
                            await templateRepo.CreateAsync(template);
                        }
                    }
                    catch (Exception)
                    {
                        // Hata durumunda atla
                    }
                }
                Log.Information("Template seed data eklendi.");
            }
            else
            {
                Log.Information("Templates tablosu zaten mevcut.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Templates tablosu oluşturma/seed hatası");
        }

        // Poses tablosunu oluştur (eğer yoksa)
        try
        {
            var conn = db.Database.GetDbConnection();
            await conn.OpenAsync();
            using var cmd = conn.CreateCommand();
            
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Poses'";
            var poseTableExists = await cmd.ExecuteScalarAsync();
            
            if (poseTableExists == null)
            {
                cmd.CommandText = @"
                    CREATE TABLE Poses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        ImagePath TEXT,
                        PromptKeyword TEXT,
                        SortOrder INTEGER NOT NULL DEFAULT 0,
                        IsActive INTEGER NOT NULL DEFAULT 1,
                        CreatedAt TEXT NOT NULL
                    );";
                await cmd.ExecuteNonQueryAsync();
                Log.Information("Poses tablosu oluşturuldu.");

                // Varsayılan pozları ekle
                var defaultPoses = new[]
                {
                    ("Ayakta", "/uploads/poses/standing.jpg", "standing pose, straight posture", 1),
                    ("Rahat", "/uploads/poses/casual.jpg", "casual relaxed pose", 2),
                    ("Editorial", "/uploads/poses/editorial.jpg", "editorial fashion pose, magazine style", 3),
                    ("Dinamik", "/uploads/poses/dynamic.jpg", "dynamic movement pose, walking", 4)
                };

                foreach (var (name, imgPath, keyword, order) in defaultPoses)
                {
                    cmd.CommandText = $@"
                        INSERT INTO Poses (Name, ImagePath, PromptKeyword, SortOrder, IsActive, CreatedAt) 
                        VALUES ('{name}', '{imgPath}', '{keyword}', {order}, 1, '{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}')";
                    await cmd.ExecuteNonQueryAsync();
                }
                Log.Information("Varsayılan pozlar eklendi.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Poses tablosu oluşturma hatası");
        }
}

// Global Exception Handler
app.UseMiddleware<GlobalExceptionHandler>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseSession(); // Session middleware (geriye dönük uyumluluk için)
app.UseAuthentication(); // JWT Authentication
app.UseAuthorization(); // Authorization
app.UseAntiforgery();

// API endpoints
app.MapControllers();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Uploads klasörünü oluştur
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
Directory.CreateDirectory(Path.Combine(uploadsPath, "generated"));

Log.Information("Giydir uygulaması başlatılıyor...");

app.Run();
