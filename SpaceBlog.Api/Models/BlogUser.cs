using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SpaceBlog.Api.Models
{
    public class BlogUser : IdentityUser
    {
        [Display(Name = "Имя для отображения")]
        [StringLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
        public string? DisplayName { get; set; }

        [Display(Name = "Имя")]
        [StringLength(50)]
        public string? FirstName { get; set; }

        [Display(Name = "Фамилия")]
        [StringLength(50)]
        public string? LastName { get; set; }

        [Display(Name = "О себе")]
        [StringLength(1000, ErrorMessage = "Описание не может быть длиннее 1000 символов")]
        public string? Bio { get; set; }

        [Display(Name = "Аватар")]
        [StringLength(500)]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Веб-сайт")]
        [StringLength(200)]
        [Url(ErrorMessage = "Некорректный URL веб-сайта")]
        public string? Website { get; set; }

        [Display(Name = "Местоположение")]
        [StringLength(100)]
        public string? Location { get; set; }

        [Display(Name = "Дата рождения")]
        [DataType(DataType.Date)]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "Дата регистрации")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Последний вход")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "Заблокирован")]
        public bool IsBanned { get; set; } = false;

        [Display(Name = "Дата блокировки")]
        public DateTime? BannedDate { get; set; }

        [Display(Name = "Причина блокировки")]
        [StringLength(500)]
        public string? BanReason { get; set; }

        // Роли управляются через Identity

        [Display(Name = "Настройки уведомлений")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Публичный профиль")]
        public bool IsPublicProfile { get; set; } = true;

        // Навигационные свойства
        public virtual ICollection<Article> Articles { get; set; } = new List<Article>();
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        // Вычисляемые свойства
        [NotMapped]
        public string FullName => $"{FirstName} {LastName}".Trim();
        
        [NotMapped]
        public int PublishedArticlesCount => Articles.Count(a => a.IsPublished);
        
        [NotMapped]
        public int TotalCommentsCount => Comments.Count();
        
        [NotMapped]
        public int Age => BirthDate.HasValue ? DateTime.Now.Year - BirthDate.Value.Year : 0;
        
        public string GetDisplayName() => !string.IsNullOrEmpty(DisplayName) ? DisplayName : FullName;

        // Бизнес-методы
        public void UpdateLastLogin()
        {
            LastLoginDate = DateTime.Now;
        }

        public void Ban(string reason)
        {
            IsBanned = true;
            BannedDate = DateTime.Now;
            BanReason = reason;
        }

        public void Unban()
        {
            IsBanned = false;
            BannedDate = null;
            BanReason = null;
        }

        // Навигационные свойства для ролей
        public virtual ICollection<IdentityUserRole<string>> UserRoles { get; set; } = new List<IdentityUserRole<string>>();

        // Бизнес-методы для работы с ролями (будут использоваться в сервисах)
        public async Task<bool> CanPublishArticles(UserManager<BlogUser> userManager)
        {
            if (IsBanned) return false;
            var roles = await userManager.GetRolesAsync(this);
            return roles.Any(r => r == Role.Names.Author || r == Role.Names.Moderator || r == Role.Names.Administrator);
        }

        public async Task<bool> CanModerateComments(UserManager<BlogUser> userManager)
        {
            if (IsBanned) return false;
            var roles = await userManager.GetRolesAsync(this);
            return roles.Any(r => r == Role.Names.Moderator || r == Role.Names.Administrator);
        }

        public async Task<bool> IsAdministrator(UserManager<BlogUser> userManager)
        {
            var roles = await userManager.GetRolesAsync(this);
            return roles.Contains(Role.Names.Administrator);
        }

        public async Task<bool> IsModerator(UserManager<BlogUser> userManager)
        {
            var roles = await userManager.GetRolesAsync(this);
            return roles.Any(r => r == Role.Names.Moderator || r == Role.Names.Administrator);
        }

        public async Task<string> GetPrimaryRole(UserManager<BlogUser> userManager)
        {
            var roles = await userManager.GetRolesAsync(this);
            
            if (roles.Contains(Role.Names.Administrator)) return Role.DisplayNames.Administrator;
            if (roles.Contains(Role.Names.Moderator)) return Role.DisplayNames.Moderator;
            if (roles.Contains(Role.Names.Author)) return Role.DisplayNames.Author;
            if (roles.Contains(Role.Names.User)) return Role.DisplayNames.User;
            
            return "Не назначена";
        }
    }
}