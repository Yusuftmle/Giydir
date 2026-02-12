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

// Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IImageOptimizationService, ImageOptimizationService>();
builder.Services.AddScoped<IPromptService, PromptService>();

// JWT Authentication
builder.Services.ConfigureAuth(builder.Configuration);

// External Services
builder.Services.AddHttpClient<IVirtualTryOnService, ReplicateVirtualTryOnService>();
builder.Services.AddHttpClient<IAIImageGenerationService, NanoBananaService>();
builder.Services.AddHttpClient<IImageDownloadService, ImageDownloadService>();

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
            var columns = new[] { "Name", "Title", "BoutiqueName", "Sector", "WebsiteUrl" };
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
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Veritabanı sütun güncelleme hatası (muhtemelen sütunlar zaten mevcut)");
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
