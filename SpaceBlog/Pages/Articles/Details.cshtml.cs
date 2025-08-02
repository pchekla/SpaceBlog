using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Articles
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public ArticleDetailViewModel? Article { get; set; }
        public IList<ArticleListItemViewModel> OtherArticles { get; set; } = new List<ArticleListItemViewModel>();
        public BlogUser? CurrentUser { get; set; }
        public IList<string> CurrentUserRoles { get; set; } = new List<string>();

        [BindProperty]
        public CommentInputModel NewComment { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class ArticleDetailViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string FormattedContent { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public string AuthorId { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsPublished { get; set; }
            public int ViewCount { get; set; }
            public List<TagViewModel> Tags { get; set; } = new();
            public List<CommentViewModel> Comments { get; set; } = new();
        }

        public class TagViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public class CommentViewModel
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public string FormattedContent { get; set; } = string.Empty;
            public string AuthorId { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsApproved { get; set; }
        }

        public class ArticleListItemViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public int ViewCount { get; set; }
        }

        public class CommentInputModel
        {
            public int ArticleId { get; set; }

            [Required(ErrorMessage = "Содержимое комментария обязательно")]
            [StringLength(2000, ErrorMessage = "Комментарий не может быть длиннее {1} символов")]
            [MinLength(10, ErrorMessage = "Комментарий должен содержать минимум {1} символов")]
            public string Content { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            try
            {
                // Загружаем текущего пользователя и его роли
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    CurrentUser = await _userManager.GetUserAsync(User);
                    if (CurrentUser != null)
                    {
                        CurrentUserRoles = await _userManager.GetRolesAsync(CurrentUser);
                    }
                }

                // Загружаем статью
                var article = await _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Include(a => a.Comments)
                        .ThenInclude(c => c.Author)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null || (!article.IsPublished && !CanEditArticle(article?.AuthorId)))
                {
                    return NotFound();
                }

                // Увеличиваем счетчик просмотров
                article!.IncrementViewCount();
                await _context.SaveChangesAsync();

                // Загружаем другие статьи автора
                var otherArticles = await _context.Articles
                    .Where(a => a.AuthorId == article.AuthorId && a.Id != article.Id && a.IsPublished)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .Select(a => new ArticleListItemViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        CreatedAt = a.CreatedAt,
                        ViewCount = a.ViewCount
                    })
                    .ToListAsync();

                // Форматируем статью
                Article = new ArticleDetailViewModel
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    FormattedContent = FormatContent(article.Content),
                    Summary = article.Summary,
                    AuthorId = article.AuthorId,
                    AuthorName = article.Author?.GetDisplayName() ?? "Неизвестный автор",
                    CreatedAt = article.CreatedAt,
                    UpdatedAt = article.UpdatedAt,
                    IsPublished = article.IsPublished,
                    ViewCount = article.ViewCount,
                    Tags = article.ArticleTags
                        .Where(at => at.Tag != null)
                        .Select(at => new TagViewModel
                        {
                            Id = at.Tag!.Id,
                            Name = at.Tag!.Name
                        }).ToList(),
                    Comments = article.Comments
                        .Where(c => c.Author != null)
                        .Select(c => new CommentViewModel
                        {
                            Id = c.Id,
                            Content = c.Content,
                            FormattedContent = FormatContent(c.Content),
                            AuthorId = c.AuthorId ?? string.Empty,
                            AuthorName = c.Author!.GetDisplayName(),
                            CreatedAt = c.CreatedAt,
                            IsApproved = c.IsApproved
                        }).ToList()
                };

                OtherArticles = otherArticles;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке статьи с ID: {ArticleId}", id);
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostAddCommentAsync()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Forbid();
                }

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Forbid();
                }

                var article = await _context.Articles
                    .FirstOrDefaultAsync(a => a.Id == NewComment.ArticleId);

                if (article == null || !article.IsPublished)
                {
                    return NotFound();
                }

                if (!ModelState.IsValid)
                {
                    await OnGetAsync(NewComment.ArticleId);
                    return Page();
                }

                var comment = new Comment
                {
                    Content = NewComment.Content.Trim(),
                    ArticleId = NewComment.ArticleId,
                    AuthorId = currentUser.Id,
                    CreatedAt = DateTime.UtcNow,
                    IsApproved = true // Автоматически одобряем комментарии
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Добавлен комментарий пользователем {UserId} к статье {ArticleId}", 
                    currentUser.Id, NewComment.ArticleId);

                StatusMessage = "Комментарий успешно добавлен.";
                return RedirectToPage(new { id = NewComment.ArticleId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении комментария");
                StatusMessage = "Произошла ошибка при добавлении комментария.";
                await OnGetAsync(NewComment.ArticleId);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var article = await _context.Articles
                    .Include(a => a.Author)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null)
                {
                    return NotFound();
                }

                if (!CanDeleteArticle(article.AuthorId))
                {
                    return Forbid();
                }

                // Удаляем связанные данные
                var articleTags = await _context.ArticleTags
                    .Where(at => at.ArticleId == id)
                    .ToListAsync();
                _context.ArticleTags.RemoveRange(articleTags);

                var comments = await _context.Comments
                    .Where(c => c.ArticleId == id)
                    .ToListAsync();
                _context.Comments.RemoveRange(comments);

                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Статья '{Title}' (ID: {Id}) удалена", article.Title, article.Id);

                StatusMessage = $"Статья \"{article.Title}\" успешно удалена.";
                return RedirectToPage("/Articles/All");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении статьи");
                StatusMessage = "Произошла ошибка при удалении статьи.";
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteCommentAsync(int commentId)
        {
            try
            {
                var comment = await _context.Comments
                    .Include(c => c.Article)
                    .FirstOrDefaultAsync(c => c.Id == commentId);

                if (comment == null)
                {
                    return NotFound();
                }

                if (!CanModerateComments())
                {
                    return Forbid();
                }

                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Комментарий (ID: {CommentId}) удален", commentId);

                StatusMessage = "Комментарий успешно удален.";
                return RedirectToPage(new { id = comment.Article?.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении комментария");
                StatusMessage = "Произошла ошибка при удалении комментария.";
                return RedirectToPage();
            }
        }

        public bool CanEditArticle(string? articleAuthorId = null)
        {
            if (CurrentUser == null) return false;
            
            var authorId = articleAuthorId ?? Article?.AuthorId;
            if (string.IsNullOrEmpty(authorId)) return false;

            // Автор может редактировать свою статью
            if (CurrentUser.Id == authorId) return true;

            // Администратор и модератор могут редактировать любые статьи
            return CurrentUserRoles.Contains(Role.Names.Administrator) ||
                   CurrentUserRoles.Contains(Role.Names.Moderator);
        }

        public bool CanDeleteArticle(string? articleAuthorId = null)
        {
            if (CurrentUser == null) return false;
            
            var authorId = articleAuthorId ?? Article?.AuthorId;
            if (string.IsNullOrEmpty(authorId)) return false;

            // Автор может удалить свою статью
            if (CurrentUser.Id == authorId) return true;

            // Администратор и модератор могут удалить любые статьи
            return CurrentUserRoles.Contains(Role.Names.Administrator) ||
                   CurrentUserRoles.Contains(Role.Names.Moderator);
        }

        public bool CanModerateComments()
        {
            if (CurrentUser == null) return false;

            // Модератор и администратор могут удалять комментарии
            return CurrentUserRoles.Contains(Role.Names.Administrator) ||
                   CurrentUserRoles.Contains(Role.Names.Moderator);
        }

        private static string FormatContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            // Простое форматирование текста
            var formatted = content
                .Replace("\r\n", "\n")
                .Replace("\n", "<br/>")
                .Replace("  ", "&nbsp;&nbsp;");

            // Можно добавить дополнительное форматирование (жирный, курсив и т.д.)
            return formatted;
        }
    }
}
