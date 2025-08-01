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

        // DbSets для бизнес-моделей
        public DbSet<Article> Articles { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }
        
        // DbSets для ролей (используются автоматически через IdentityDbContext)
        // public DbSet<Role> Roles уже наследуется от IdentityDbContext

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка BlogUser
            modelBuilder.Entity<BlogUser>(entity =>
            {
                entity.HasIndex(u => u.DisplayName);
                entity.Property(u => u.RegistrationDate).HasDefaultValueSql("datetime('now')");
                
                // Связь с ролями через IdentityUserRole
                entity.HasMany(u => u.UserRoles)
                      .WithOne()
                      .HasForeignKey(ur => ur.UserId)
                      .IsRequired();
            });

            // Настройка Role
            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasIndex(r => r.Name).IsUnique();
                entity.HasIndex(r => r.IsActive);
                entity.Property(r => r.CreatedAt).HasDefaultValueSql("datetime('now')");
                
                // Связь с пользователями через IdentityUserRole
                entity.HasMany(r => r.UserRoles)
                      .WithOne()
                      .HasForeignKey(ur => ur.RoleId)
                      .IsRequired();
            });

            // Настройка Article
            modelBuilder.Entity<Article>(entity =>
            {
                entity.HasIndex(a => a.Title);
                entity.HasIndex(a => a.Slug).IsUnique();
                entity.HasIndex(a => a.CreatedAt);
                entity.HasIndex(a => a.IsPublished);
                
                entity.Property(a => a.CreatedAt).HasDefaultValueSql("datetime('now')");
                
                entity.HasOne(a => a.Author)
                      .WithMany(u => u.Articles)
                      .HasForeignKey(a => a.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Настройка Tag
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasIndex(t => t.Name).IsUnique();
                entity.HasIndex(t => t.Slug).IsUnique();
                entity.HasIndex(t => t.IsActive);
                
                entity.Property(t => t.CreatedAt).HasDefaultValueSql("datetime('now')");
                
                entity.HasOne(t => t.CreatedBy)
                      .WithMany()
                      .HasForeignKey(t => t.CreatedById)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Настройка Comment
            modelBuilder.Entity<Comment>(entity =>
            {
                entity.HasIndex(c => c.CreatedAt);
                entity.HasIndex(c => c.IsApproved);
                entity.HasIndex(c => c.IsBlocked);
                
                entity.Property(c => c.CreatedAt).HasDefaultValueSql("datetime('now')");
                
                entity.HasOne(c => c.Article)
                      .WithMany(a => a.Comments)
                      .HasForeignKey(c => c.ArticleId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(c => c.Author)
                      .WithMany(u => u.Comments)
                      .HasForeignKey(c => c.AuthorId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasOne(c => c.ModeratedBy)
                      .WithMany()
                      .HasForeignKey(c => c.ModeratedById)
                      .OnDelete(DeleteBehavior.SetNull);
                
                // Настройка для вложенных комментариев
                entity.HasOne(c => c.ParentComment)
                      .WithMany(c => c.Replies)
                      .HasForeignKey(c => c.ParentCommentId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Настройка связей ArticleTag (many-to-many)
            modelBuilder.Entity<ArticleTag>(entity =>
            {
                entity.HasKey(at => new { at.ArticleId, at.TagId });
                
                entity.HasIndex(at => at.CreatedAt);
                entity.HasIndex(at => at.Order);
                
                entity.Property(at => at.CreatedAt).HasDefaultValueSql("datetime('now')");
                
                entity.HasOne(at => at.Article)
                      .WithMany(a => a.ArticleTags)
                      .HasForeignKey(at => at.ArticleId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(at => at.Tag)
                      .WithMany(t => t.ArticleTags)
                      .HasForeignKey(at => at.TagId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(at => at.CreatedBy)
                      .WithMany()
                      .HasForeignKey(at => at.CreatedById)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Дополнительные настройки для производительности
            modelBuilder.Entity<Article>()
                .Navigation(a => a.Comments)
                .EnableLazyLoading(false);
            
            modelBuilder.Entity<Article>()
                .Navigation(a => a.ArticleTags)
                .EnableLazyLoading(false);
        }
    }
}
