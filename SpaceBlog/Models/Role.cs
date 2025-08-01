using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace SpaceBlog.Models
{
    public class Role : IdentityRole
    {
        [Display(Name = "Описание роли")]
        [StringLength(500, ErrorMessage = "Описание роли не может быть длиннее 500 символов")]
        public string? Description { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Активна")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Приоритет роли")]
        public int Priority { get; set; } = 0;

        // Навигационные свойства
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();

        // Предопределенные роли
        public static class Names
        {
            public const string Administrator = "Administrator";
            public const string Moderator = "Moderator";
            public const string Author = "Author";
            public const string User = "User";
        }

        public static class DisplayNames
        {
            public const string Administrator = "Администратор";
            public const string Moderator = "Модератор";
            public const string Author = "Автор";
            public const string User = "Пользователь";
        }

        // Статические роли
        public static readonly Role AdministratorRole = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = Names.Administrator,
            NormalizedName = Names.Administrator.ToUpper(),
            Description = "Полные права администратора системы",
            Priority = 100,
            IsActive = true
        };

        public static readonly Role ModeratorRole = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = Names.Moderator,
            NormalizedName = Names.Moderator.ToUpper(),
            Description = "Права модератора контента",
            Priority = 75,
            IsActive = true
        };

        public static readonly Role AuthorRole = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = Names.Author,
            NormalizedName = Names.Author.ToUpper(),
            Description = "Права автора статей",
            Priority = 50,
            IsActive = true
        };

        public static readonly Role UserRole = new()
        {
            Id = Guid.NewGuid().ToString(),
            Name = Names.User,
            NormalizedName = Names.User.ToUpper(),
            Description = "Базовые права пользователя",
            Priority = 25,
            IsActive = true
        };

        // Бизнес-методы
        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }

        public bool IsAdministrator()
        {
            return Name == Names.Administrator;
        }

        public bool IsModerator()
        {
            return Name == Names.Moderator || IsAdministrator();
        }

        public bool IsAuthor()
        {
            return Name == Names.Author || IsModerator();
        }
    }
}