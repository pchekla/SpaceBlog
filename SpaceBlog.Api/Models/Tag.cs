using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpaceBlog.Api.Models
{
    public class Tag
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Название тега обязательно")]
        [StringLength(50, ErrorMessage = "Название тега не может быть длиннее 50 символов")]
        [Display(Name = "Название тега")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Описание не может быть длиннее 200 символов")]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Display(Name = "URL тега (slug)")]
        [StringLength(60)]
        public string? Slug { get; set; }

        [Display(Name = "Цвет тега")]
        [StringLength(7)]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Некорректный формат цвета")]
        public string? Color { get; set; } = "#6c757d";

        [Display(Name = "Иконка тега")]
        [StringLength(50)]
        public string? Icon { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Порядок сортировки")]
        public int SortOrder { get; set; } = 0;

        [Display(Name = "Создатель тега")]
        public string? CreatedById { get; set; }
        public BlogUser? CreatedBy { get; set; }

        [Display(Name = "Количество просмотров")]
        public int ViewCount { get; set; } = 0;

        // Навигационные свойства
        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();

        // Вычисляемое свойство для количества использований
        [NotMapped]
        public int UsageCount => ArticleTags?.Count ?? 0;

        // Вычисляемые свойства
        [NotMapped]
        public IEnumerable<Article> Articles => ArticleTags.Select(at => at.Article).Where(a => a != null)!;
        
        [NotMapped]
        public IEnumerable<Article> PublishedArticles => Articles.Where(a => a.IsPublished);
        
        [NotMapped]
        public int ArticlesCount => PublishedArticles.Count();

        // Бизнес-методы
        public string GenerateSlug()
        {
            if (string.IsNullOrEmpty(Name))
                return string.Empty;

            return Name.ToLowerInvariant()
                      .Replace(" ", "-")
                      .Replace("ё", "e")
                      .Replace("а", "a")
                      .Replace("б", "b")
                      .Replace("в", "v")
                      .Replace("г", "g")
                      .Replace("д", "d")
                      .Replace("е", "e")
                      .Replace("ж", "zh")
                      .Replace("з", "z")
                      .Replace("и", "i")
                      .Replace("й", "y")
                      .Replace("к", "k")
                      .Replace("л", "l")
                      .Replace("м", "m")
                      .Replace("н", "n")
                      .Replace("о", "o")
                      .Replace("п", "p")
                      .Replace("р", "r")
                      .Replace("с", "s")
                      .Replace("т", "t")
                      .Replace("у", "u")
                      .Replace("ф", "f")
                      .Replace("х", "h")
                      .Replace("ц", "c")
                      .Replace("ч", "ch")
                      .Replace("ш", "sh")
                      .Replace("щ", "sch")
                      .Replace("ъ", "")
                      .Replace("ы", "y")
                      .Replace("ь", "")
                      .Replace("э", "e")
                      .Replace("ю", "yu")
                      .Replace("я", "ya");
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}