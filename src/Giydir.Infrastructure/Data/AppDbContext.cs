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

        // Seed data - Örnek model pozları
        modelBuilder.Entity<ModelAsset>().HasData(
            new ModelAsset
            {
                Id = "female-casual-1",
                Name = "Kadın - Casual Poz 1",
                ThumbnailPath = "/assets/models/female-casual-1.jpg",
                FullImagePath = "/assets/models/female-casual-1.jpg",
                Gender = "Female",
                Category = "upper_body"
            },
            new ModelAsset
            {
                Id = "female-casual-2",
                Name = "Kadın - Casual Poz 2",
                ThumbnailPath = "/assets/models/female-casual-2.jpg",
                FullImagePath = "/assets/models/female-casual-2.jpg",
                Gender = "Female",
                Category = "upper_body"
            },
            new ModelAsset
            {
                Id = "female-dress-1",
                Name = "Kadın - Elbise Poz 1",
                ThumbnailPath = "/assets/models/female-dress-1.jpg",
                FullImagePath = "/assets/models/female-dress-1.jpg",
                Gender = "Female",
                Category = "dresses"
            },
            new ModelAsset
            {
                Id = "male-casual-1",
                Name = "Erkek - Casual Poz 1",
                ThumbnailPath = "/assets/models/male-casual-1.jpg",
                FullImagePath = "/assets/models/male-casual-1.jpg",
                Gender = "Male",
                Category = "upper_body"
            },
            new ModelAsset
            {
                Id = "male-casual-2",
                Name = "Erkek - Casual Poz 2",
                ThumbnailPath = "/assets/models/male-casual-2.jpg",
                FullImagePath = "/assets/models/male-casual-2.jpg",
                Gender = "Male",
                Category = "upper_body"
            }
        );

        // Seed default user
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                Email = "demo@giydir.com",
                PasswordHash = "demo123", // MVP - düz metin, sonra hash'e geçilecek
                Credits = 50,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );

        // Seed default project
        modelBuilder.Entity<Project>().HasData(
            new Project
            {
                Id = 1,
                UserId = 1,
                Name = "İlk Projem",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}

