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
builder.Services.AddHttpContextAccessor();

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IModelAssetRepository, ModelAssetRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IGeneratedImageRepository, GeneratedImageRepository>();

// Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<IImageOptimizationService, ImageOptimizationService>();

// JWT Authentication
builder.Services.ConfigureAuth(builder.Configuration);

// External Services
builder.Services.AddHttpClient<IVirtualTryOnService, ReplicateVirtualTryOnService>();
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
