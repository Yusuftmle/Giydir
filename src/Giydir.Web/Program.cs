using Giydir.Core.Interfaces;
using Giydir.Infrastructure.Data;
using Giydir.Infrastructure.ExternalServices;
using Giydir.Infrastructure.Repositories;
using Giydir.Web.Components;
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

// Repositories
builder.Services.AddScoped<IModelAssetRepository, ModelAssetRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IGeneratedImageRepository, GeneratedImageRepository>();

// External Services
builder.Services.AddHttpClient<IVirtualTryOnService, ReplicateVirtualTryOnService>();

// HttpClient for Blazor pages (internal API calls)
builder.Services.AddScoped(sp =>
{
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(builder.Configuration["BaseUrl"] ?? "http://localhost:5000");
    return httpClient;
});

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
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

// API endpoints
app.MapControllers();

// Blazor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Uploads klasörünü oluştur
var uploadsPath = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

Log.Information("Giydir uygulaması başlatılıyor...");

app.Run();
