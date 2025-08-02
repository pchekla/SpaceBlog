using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpaceBlog.Api.Models
{
    public class Article
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Заголовок обязателен")]
        [StringLength(200, ErrorMessage = "Заголовок не может быть длиннее 200 символов")]
        [Display(Name = "Заголовок")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Содержание обязательно")]
        [Display(Name = "Содержание статьи")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Краткое описание не может быть длиннее 500 символов")]
        [Display(Name = "Краткое описание")]
        public string? Summary { get; set; }

        [Display(Name = "URL статьи (slug)")]
        [StringLength(250)]
        public string? Slug { get; set; }

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Дата обновления")]
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;
        public BlogUser? Author { get; set; }

        [Display(Name = "Опубликовано")]
        public bool IsPublished { get; set; } = false;

        [Display(Name = "Дата публикации")]
        public DateTime? PublishedAt { get; set; }

        [Display(Name = "Количество просмотров")]
        public int ViewCount { get; set; } = 0;

        [Display(Name = "Изображение статьи")]
        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [Display(Name = "Мета-описание для SEO")]
        [StringLength(160)]
        public string? MetaDescription { get; set; }

        [Display(Name = "Ключевые слова")]
        [StringLength(200)]
        public string? Keywords { get; set; }

        // Навигационные свойства
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public virtual ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();

        // Вычисляемые свойства
        [NotMapped]
        public IEnumerable<Tag> Tags => ArticleTags.Select(at => at.Tag).Where(t => t != null)!;
        
        [NotMapped]
        public IEnumerable<Comment> PublishedComments => Comments.Where(c => c.IsApproved);
        
        [NotMapped]
        public int CommentsCount => PublishedComments.Count();

        // Бизнес-методы
        public void Publish()
        {
            IsPublished = true;
            PublishedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        public void Unpublish()
        {
            IsPublished = false;
            PublishedAt = null;
            UpdatedAt = DateTime.Now;
        }

        public void IncrementViewCount()
        {
            ViewCount++;
        }

        public string GenerateSlug()
        {
            if (string.IsNullOrEmpty(Title))
                return string.Empty;

            return Title.ToLowerInvariant()
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
    }
}