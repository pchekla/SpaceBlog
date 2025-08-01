using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Models
{
    public class ArticleTag
    {
        [Required]
        public int ArticleId { get; set; }
        public Article? Article { get; set; }

        [Required]
        public int TagId { get; set; }
        public Tag? Tag { get; set; }

        [Display(Name = "Дата добавления")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Добавил")]
        public string? CreatedById { get; set; }
        public BlogUser? CreatedBy { get; set; }

        [Display(Name = "Порядок отображения")]
        public int Order { get; set; } = 0;

        // Бизнес-методы
        public void SetOrder(int order)
        {
            Order = order;
        }
    }
}