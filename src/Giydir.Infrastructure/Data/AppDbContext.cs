using Giydir.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Giydir.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<GeneratedImage> GeneratedImages => Set<GeneratedImage>();
    public DbSet<ModelAsset> ModelAssets => Set<ModelAsset>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<SavedPrompt> SavedPrompts => Set<SavedPrompt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Credits).HasDefaultValue(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.Property(e => e.BoutiqueName).HasMaxLength(200);
            entity.Property(e => e.Sector).HasMaxLength(100);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
        });

        // Project
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Projects)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // GeneratedImage
        modelBuilder.Entity<GeneratedImage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalClothingPath).IsRequired();
            entity.Property(e => e.ModelAssetId).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Images)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.ModelAsset)
                  .WithMany(m => m.GeneratedImages)
                  .HasForeignKey(e => e.ModelAssetId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ModelAsset
        modelBuilder.Entity<ModelAsset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Gender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
        });

        // Template
        modelBuilder.Entity<Template>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Style).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(50);
            entity.Property(e => e.Pattern).HasMaxLength(50);
            entity.Property(e => e.Material).HasMaxLength(50);
            entity.Property(e => e.PromptTemplate).HasMaxLength(1000);
        });

        // Seed data - Users
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Email = "admin@giydir.ai",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminGiydir2024!"),
                Credits = 9999,
                Role = "Admin",
                Name = "Giydir Admin",
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 2,
                Email = "demo@giydir.com",
                PasswordHash = "demo123", // MVP - düz metin, sonra hash'e geçilecek
                Credits = 50,
                Name = "Demo Kullanıcı",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default project
        modelBuilder.Entity<Project>().HasData(
            new Project
            {
                Id = 1,
                UserId = 2,
                Name = "İlk Projem",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed templates
        modelBuilder.Entity<Template>().HasData(
            new Template
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
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Template
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
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Template
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
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Template
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
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}

