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

        // Seed data - Stitch tasarımından model pozları
        modelBuilder.Entity<ModelAsset>().HasData(
            new ModelAsset
            {
                Id = "elena-s",
                Name = "Elena S.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuBi-JZTVchJ7MDg0T4cmwa7gac-HLXK1ubWKFs8d0CEzRbuEmZZBjol8-E2_0nynXHYYBWDZ18Da81JWBgzL89oRYxxHoYGkK_X_AoUBzPPRH1fslzv_CZSzHHTl0eEdiYwPIr4_AJiVF1qbkcS1uoc7QnPlQkV5ikxMhCWx1wDDIkT5-VmuVgKMv9RHtygmjMP614JOHRWUhXlz7WzCkzxdz5w5eP6nYau5gbOaSYsMgGV2GvNwU-0IBuCF6NxHBZfyP3UQ7Ye9WoD",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuBi-JZTVchJ7MDg0T4cmwa7gac-HLXK1ubWKFs8d0CEzRbuEmZZBjol8-E2_0nynXHYYBWDZ18Da81JWBgzL89oRYxxHoYGkK_X_AoUBzPPRH1fslzv_CZSzHHTl0eEdiYwPIr4_AJiVF1qbkcS1uoc7QnPlQkV5ikxMhCWx1wDDIkT5-VmuVgKMv9RHtygmjMP614JOHRWUhXlz7WzCkzxdz5w5eP6nYau5gbOaSYsMgGV2GvNwU-0IBuCF6NxHBZfyP3UQ7Ye9WoD",
                Gender = "Female",
                Category = "Studio / Casual"
            },
            new ModelAsset
            {
                Id = "marcus-j",
                Name = "Marcus J.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuAeXc4bOfxLbkVLRqH-4051XnkJqMoSi0DqAgiEV1SsI0sdvwv_Vq3P3CRl7TmzS0RblYW6cVn7yQQxbmBCBEuGDpuKls8IMwDOiQo0RbN7xbcGOar_N58kqg8ih0NbaTh3LNFH8KuFBQDlyTOTay1LGiKtPWzuf_bWF_Zop6TfzYxylk0XYPoqXXRzwPWOjK0z99e0gGuefuSmMSvCUCRtR59cBKKfeE-dlDe9ghuirK4cNa25d3llmDNFhipCxav3Sb5ZV8LoSI-E",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuAeXc4bOfxLbkVLRqH-4051XnkJqMoSi0DqAgiEV1SsI0sdvwv_Vq3P3CRl7TmzS0RblYW6cVn7yQQxbmBCBEuGDpuKls8IMwDOiQo0RbN7xbcGOar_N58kqg8ih0NbaTh3LNFH8KuFBQDlyTOTay1LGiKtPWzuf_bWF_Zop6TfzYxylk0XYPoqXXRzwPWOjK0z99e0gGuefuSmMSvCUCRtR59cBKKfeE-dlDe9ghuirK4cNa25d3llmDNFhipCxav3Sb5ZV8LoSI-E",
                Gender = "Male",
                Category = "Street / Urban"
            },
            new ModelAsset
            {
                Id = "sophia-l",
                Name = "Sophia L.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuAlm9ZnHv3XtmLsmCiE3BVixApLiZMOQQdxDL5p5s8XuWtINXFvePRxvb8cUJSMjPewgmI7KxhhZ4HDO05TkDeLrkXzZwDHElUSf-2Z3iqpnnGoW3cfdX8p-6rKBtb-iu8gMVI3-Xcauke-Ro6DNyQ7KtS6P84UoaY_Zrww-vicwSjDHtld1Z2SlPGVz7flzgID5kTv33hkULsiw7lwA4A6iRoZXLhlGcw_s4Nes6ihxduFL0lLJjV1USq8U7mVar20nDNdiHxqjKZQ",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuAlm9ZnHv3XtmLsmCiE3BVixApLiZMOQQdxDL5p5s8XuWtINXFvePRxvb8cUJSMjPewgmI7KxhhZ4HDO05TkDeLrkXzZwDHElUSf-2Z3iqpnnGoW3cfdX8p-6rKBtb-iu8gMVI3-Xcauke-Ro6DNyQ7KtS6P84UoaY_Zrww-vicwSjDHtld1Z2SlPGVz7flzgID5kTv33hkULsiw7lwA4A6iRoZXLhlGcw_s4Nes6ihxduFL0lLJjV1USq8U7mVar20nDNdiHxqjKZQ",
                Gender = "Female",
                Category = "Outdoor / Lifestyle"
            },
            new ModelAsset
            {
                Id = "chloe-r",
                Name = "Chloe R.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuCzEae11HajK_A_XmyLtq50GnrZLqapqrXihXSrgQIOuj6vrFac-tjqr0hYtOQI24MuIDhf2MbozJtGWIiLHQ2tAOq-crvzaC-HZe-37hH7lf4nfEMyXpg0e6QBDsLys2BQurKITZB5BFbxn2aokABhYTEnUx7xZ7IZ2bIiC-1dDyWAA9PhqZpqcMiBmT1UbWHsqYUB9HMitkSCryuwTopPez7vRIi9ErUPChEjSLT7FR5y7Ng9sbsqD239FB_exp97HmaKkVWvB13n",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuCzEae11HajK_A_XmyLtq50GnrZLqapqrXihXSrgQIOuj6vrFac-tjqr0hYtOQI24MuIDhf2MbozJtGWIiLHQ2tAOq-crvzaC-HZe-37hH7lf4nfEMyXpg0e6QBDsLys2BQurKITZB5BFbxn2aokABhYTEnUx7xZ7IZ2bIiC-1dDyWAA9PhqZpqcMiBmT1UbWHsqYUB9HMitkSCryuwTopPez7vRIi9ErUPChEjSLT7FR5y7Ng9sbsqD239FB_exp97HmaKkVWvB13n",
                Gender = "Female",
                Category = "Fashion / High End"
            },
            new ModelAsset
            {
                Id = "david-k",
                Name = "David K.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuD6DuXr3x3X79t_l1NkJMNLEHauz5fde4eTdt0b8KCYo0lhdNxzwjAg42EYaFZliu3eJP1z1y0Z5179XQHk_7yv8EM9dM0v9aWC3fEyMd-4I6RjcjPe3U8Jh0DV-e-avN-h-FzMqX1ku2RlMlEUd7fp-81ypo1JH0AkRZb-JbmId3fIIQufQY0psa1yueck3CglOpriLMSkbgwqXgrf9FviwNQ2pU0P2p28Ebffl9gCLMV4AD0xe1FyrNujVfgLplmMkdFXllF1Vlg8",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuD6DuXr3x3X79t_l1NkJMNLEHauz5fde4eTdt0b8KCYo0lhdNxzwjAg42EYaFZliu3eJP1z1y0Z5179XQHk_7yv8EM9dM0v9aWC3fEyMd-4I6RjcjPe3U8Jh0DV-e-avN-h-FzMqX1ku2RlMlEUd7fp-81ypo1JH0AkRZb-JbmId3fIIQufQY0psa1yueck3CglOpriLMSkbgwqXgrf9FviwNQ2pU0P2p28Ebffl9gCLMV4AD0xe1FyrNujVfgLplmMkdFXllF1Vlg8",
                Gender = "Male",
                Category = "Casual / Everyday"
            },
            new ModelAsset
            {
                Id = "maya-t",
                Name = "Maya T.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuDyg_lxWaOrDYttj5wgPZ5_m0ZaHr_5n9j2MMmJCFpuHNaoz5kO618aRaYDB-ic6EKjDZh8mOZXTgPgXlYRKAaIRMLo7Np5S4-t2TgCdA56aBOs5gkMmZHb9n7yvceRlGJ77BUUDdHqEWbvBmNs6DXYz1o2QAJURjYieclJRbmDf_Y_2QUjT2P32rnG1qwfkb6yzoX7WApYusE21mLRpQ-a_2Z1zHxYWIYj66Y2VkenKsk5Vz9LYl0UZjoAqvu_0nKnaQ_JbXI5zirT",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuDyg_lxWaOrDYttj5wgPZ5_m0ZaHr_5n9j2MMmJCFpuHNaoz5kO618aRaYDB-ic6EKjDZh8mOZXTgPgXlYRKAaIRMLo7Np5S4-t2TgCdA56aBOs5gkMmZHb9n7yvceRlGJ77BUUDdHqEWbvBmNs6DXYz1o2QAJURjYieclJRbmDf_Y_2QUjT2P32rnG1qwfkb6yzoX7WApYusE21mLRpQ-a_2Z1zHxYWIYj66Y2VkenKsk5Vz9LYl0UZjoAqvu_0nKnaQ_JbXI5zirT",
                Gender = "Female",
                Category = "Beauty / Close-up"
            },
            new ModelAsset
            {
                Id = "zara-b",
                Name = "Zara B.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuBk9i1u7KLFftSujohtoBwJuGdGFqUmTJ4s90JITB6TnZeTG4QcMZTgu3rF5LQvTQ-mMbwc_kHNl-yh1gc15SjUPQASoGvZfBc72HAkimL0FFCbmgRDcGOHkcKZ0bGtOe8mcrjfFY6kwGXy4mkO6iEnP-H8OyLw21CUW3eULNPm8T4fJRYEG7N5xHj7XQsyyoaZstq2oTBa_6vywGTk5X9E4RTjwMjU-NiNQkKQcUk8pyyqXoQCma9osZvM_UPDs2JPKSx87_ei4VjQ",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuBk9i1u7KLFftSujohtoBwJuGdGFqUmTJ4s90JITB6TnZeTG4QcMZTgu3rF5LQvTQ-mMbwc_kHNl-yh1gc15SjUPQASoGvZfBc72HAkimL0FFCbmgRDcGOHkcKZ0bGtOe8mcrjfFY6kwGXy4mkO6iEnP-H8OyLw21CUW3eULNPm8T4fJRYEG7N5xHj7XQsyyoaZstq2oTBa_6vywGTk5X9E4RTjwMjU-NiNQkKQcUk8pyyqXoQCma9osZvM_UPDs2JPKSx87_ei4VjQ",
                Gender = "Female",
                Category = "Business / Formal"
            },
            new ModelAsset
            {
                Id = "lily-a",
                Name = "Lily A.",
                ThumbnailPath = "https://lh3.googleusercontent.com/aida-public/AB6AXuAtFeABsVMgskm3n1brapolfui2kgEyVi6Ve1O5N2NcVD_6-GJo9knrL1sk3sgwC7kvcumYtTqZkxDce8LhtxWBY-62Wp33k68sGgXqiYsj_q3O4tYZxYPawrImixcvc2a-tWQD93mxBnbBfm7Vt0sm3Muq_q3JbEcf-wEQUn5kSf3WahGfBz762URViP8xpM0F1hFL1KawI6bbGPNmyYgoHfOCttstNkd7tP_XpTIhlWY3yy-lR-oKQ7YDE4Ce4qgeGv95nzXbKow3",
                FullImagePath = "https://lh3.googleusercontent.com/aida-public/AB6AXuAtFeABsVMgskm3n1brapolfui2kgEyVi6Ve1O5N2NcVD_6-GJo9knrL1sk3sgwC7kvcumYtTqZkxDce8LhtxWBY-62Wp33k68sGgXqiYsj_q3O4tYZxYPawrImixcvc2a-tWQD93mxBnbBfm7Vt0sm3Muq_q3JbEcf-wEQUn5kSf3WahGfBz762URViP8xpM0F1hFL1KawI6bbGPNmyYgoHfOCttstNkd7tP_XpTIhlWY3yy-lR-oKQ7YDE4Ce4qgeGv95nzXbKow3",
                Gender = "Female",
                Category = "Artistic / Conceptual"
            },
            // AI Generated işlemleri için dummy model
            new ModelAsset
            {
                Id = "ai-generated",
                Name = "AI Generated",
                ThumbnailPath = "/images/ai-placeholder.png",
                FullImagePath = "/images/ai-placeholder.png",
                Gender = "Unisex",
                Category = "AI"
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
                Name = "Demo Kullanıcı",
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

