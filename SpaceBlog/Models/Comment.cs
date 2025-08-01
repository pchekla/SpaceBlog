using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpaceBlog.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Содержание комментария обязательно")]
        [StringLength(1000, ErrorMessage = "Комментарий не может быть длиннее 1000 символов")]
        [Display(Name = "Текст комментария")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Дата создания")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Дата обновления")]
        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string AuthorId { get; set; } = string.Empty;
        public BlogUser? Author { get; set; }

        [Required]
        public int ArticleId { get; set; }
        public Article? Article { get; set; }

        [Display(Name = "Одобрен")]
        public bool IsApproved { get; set; } = false;

        [Display(Name = "Заблокирован")]
        public bool IsBlocked { get; set; } = false;

        [Display(Name = "Дата модерации")]
        public DateTime? ModeratedAt { get; set; }

        [Display(Name = "Модератор")]
        public string? ModeratedById { get; set; }
        public BlogUser? ModeratedBy { get; set; }

        [Display(Name = "Причина блокировки")]
        [StringLength(500)]
        public string? BlockReason { get; set; }

        [Display(Name = "IP-адрес")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Display(Name = "User Agent")]
        [StringLength(500)]
        public string? UserAgent { get; set; }

        [Display(Name = "Родительский комментарий")]
        public int? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }

        // Навигационные свойства
        public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();

        // Вычисляемые свойства
        [NotMapped]
        public bool IsModerated => IsApproved || IsBlocked;
        
        [NotMapped]
        public CommentStatus Status => IsBlocked ? CommentStatus.Blocked : 
                                     IsApproved ? CommentStatus.Approved : 
                                     CommentStatus.Pending;
        
        [NotMapped]
        public bool IsReply => ParentCommentId.HasValue;
        
        [NotMapped]
        public int RepliesCount => Replies.Count(r => r.IsApproved);

        // Бизнес-методы
        public void Approve(string moderatorId)
        {
            IsApproved = true;
            IsBlocked = false;
            ModeratedAt = DateTime.Now;
            ModeratedById = moderatorId;
            BlockReason = null;
        }

        public void Block(string moderatorId, string reason)
        {
            IsBlocked = true;
            IsApproved = false;
            ModeratedAt = DateTime.Now;
            ModeratedById = moderatorId;
            BlockReason = reason;
        }

        public void UpdateContent(string newContent)
        {
            Content = newContent;
            UpdatedAt = DateTime.Now;
        }

        public bool CanBeEditedBy(string userId)
        {
            return AuthorId == userId && (DateTime.Now - CreatedAt).TotalMinutes <= 15;
        }

        public bool CanBeDeletedBy(string userId, IList<string> userRoles)
        {
            return AuthorId == userId || 
                   userRoles.Contains(Role.Names.Moderator) || 
                   userRoles.Contains(Role.Names.Administrator);
        }
    }

    public enum CommentStatus
    {
        [Display(Name = "Ожидает модерации")]
        Pending = 0,
        
        [Display(Name = "Одобрен")]
        Approved = 1,
        
        [Display(Name = "Заблокирован")]
        Blocked = 2
    }
}