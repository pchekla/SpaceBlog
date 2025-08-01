using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Models;

namespace SpaceBlog.Data
{
    public class ApplicationDbContext : IdentityDbContext<BlogUser, Role, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets для наших моделей
        public DbSet<Article> Articles { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Comment> Comments { get; set; } = null!;
        public DbSet<ArticleTag> ArticleTags { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Конфигурация BlogUser
            builder.Entity<BlogUser>(entity =>
            {
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.Bio).HasMaxLength(1000);
                entity.Property(u => u.Website).HasMaxLength(200);
                entity.Property(u => u.Location).HasMaxLength(100);
                entity.Property(u => u.AvatarUrl).HasMaxLength(500);
            });

            // Конфигурация Article
            builder.Entity<Article>(entity =>
            {
                entity.HasKey(a => a.Id);
                entity.Property(a => a.Title).IsRequired().HasMaxLength(200);
                entity.Property(a => a.Slug).HasMaxLength(250);
                entity.Property(a => a.Summary).HasMaxLength(500);
                entity.Property(a => a.Content).IsRequired();
                entity.Property(a => a.MetaDescription).HasMaxLength(500);
                entity.Property(a => a.ImageUrl).HasMaxLength(500);
                entity.Property(a => a.Keywords).HasMaxLength(200);

                // Индексы
                entity.HasIndex(a => a.Slug).IsUnique();
                entity.HasIndex(a => a.IsPublished);
                entity.HasIndex(a => a.CreatedAt);

                // Связи
                entity.HasOne(a => a.Author)
                    .WithMany(u => u.Articles)
                    .HasForeignKey(a => a.AuthorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(a => a.Comments)
                    .WithOne(c => c.Article)
                    .HasForeignKey(c => c.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(a => a.ArticleTags)
                    .WithOne(at => at.Article)
                    .HasForeignKey(at => at.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация Tag
            builder.Entity<Tag>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired().HasMaxLength(100);
                entity.Property(t => t.Slug).IsRequired().HasMaxLength(120);
                entity.Property(t => t.Description).HasMaxLength(500);
                entity.Property(t => t.Color).HasMaxLength(7);

                // Индексы
                entity.HasIndex(t => t.Slug).IsUnique();
                entity.HasIndex(t => t.Name);

                // Связи
                entity.HasMany(t => t.ArticleTags)
                    .WithOne(at => at.Tag)
                    .HasForeignKey(at => at.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация Comment
            builder.Entity<Comment>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Content).IsRequired().HasMaxLength(1000);
                entity.Property(c => c.IpAddress).HasMaxLength(45);
                entity.Property(c => c.UserAgent).HasMaxLength(500);
                entity.Property(c => c.BlockReason).HasMaxLength(500);

                // Индексы
                entity.HasIndex(c => c.CreatedAt);
                entity.HasIndex(c => c.IsApproved);

                // Связи
                entity.HasOne(c => c.Article)
                    .WithMany(a => a.Comments)
                    .HasForeignKey(c => c.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(c => c.Author)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(c => c.AuthorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Конфигурация ArticleTag (связь многие-ко-многим)
            builder.Entity<ArticleTag>(entity =>
            {
                entity.HasKey(at => new { at.ArticleId, at.TagId });

                entity.HasOne(at => at.Article)
                    .WithMany(a => a.ArticleTags)
                    .HasForeignKey(at => at.ArticleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(at => at.Tag)
                    .WithMany(t => t.ArticleTags)
                    .HasForeignKey(at => at.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Конфигурация Role
            builder.Entity<Role>(entity =>
            {
                entity.Property(r => r.Description).HasMaxLength(500);
            });

            // Убираем дублирующие связи в IdentityUserRole
            builder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.ToTable("AspNetUserRoles");
            });
        }
    }
}