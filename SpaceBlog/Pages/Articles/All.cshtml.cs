using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Articles
{
    public class AllModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<AllModel> _logger;

        public AllModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<AllModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<ArticleDisplayViewModel> Articles { get; set; } = new List<ArticleDisplayViewModel>();
        public BlogUser? CurrentUser { get; set; }
        public IList<string> CurrentUserRoles { get; set; } = new List<string>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class ArticleDisplayViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public string AuthorId { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public bool IsPublished { get; set; }
            public int ViewCount { get; set; }
            public int CommentsCount { get; set; }
            public List<TagViewModel> Tags { get; set; } = new();
        }

        public class TagViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
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

                // Загружаем статьи
                var articles = await _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Include(a => a.Comments)
                    .Where(a => a.IsPublished) // Показываем только опубликованные статьи
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var articleViewModels = new List<ArticleDisplayViewModel>();

                foreach (var article in articles)
                {
                    var tags = article.ArticleTags
                        .Where(at => at.Tag != null)
                        .Select(at => new TagViewModel
                        {
                            Id = at.Tag!.Id,
                            Name = at.Tag!.Name
                        })
                        .ToList();

                    articleViewModels.Add(new ArticleDisplayViewModel
                    {
                        Id = article.Id,
                        Title = article.Title,
                        Content = article.Content,
                        Summary = article.Summary,
                        AuthorId = article.AuthorId,
                        AuthorName = article.Author?.GetDisplayName() ?? "Неизвестный автор",
                        CreatedAt = article.CreatedAt,
                        UpdatedAt = article.UpdatedAt,
                        IsPublished = article.IsPublished,
                        ViewCount = article.ViewCount,
                        CommentsCount = article.Comments.Count(c => c.IsApproved),
                        Tags = tags
                    });
                }

                Articles = articleViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка всех статей");
                Articles = new List<ArticleDisplayViewModel>();
                StatusMessage = "Произошла ошибка при загрузке статей.";
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
                    StatusMessage = "Статья не найдена.";
                    return RedirectToPage();
                }

                // Проверяем права на удаление
                if (!CanDeleteArticle(article.AuthorId))
                {
                    StatusMessage = "У вас нет прав для удаления этой статьи.";
                    return RedirectToPage();
                }

                // Удаляем связанные ArticleTags
                var articleTags = await _context.ArticleTags
                    .Where(at => at.ArticleId == id)
                    .ToListAsync();
                _context.ArticleTags.RemoveRange(articleTags);

                // Удаляем связанные комментарии
                var comments = await _context.Comments
                    .Where(c => c.ArticleId == id)
                    .ToListAsync();
                _context.Comments.RemoveRange(comments);

                // Удаляем статью
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Статья '{article.Title}' (ID: {article.Id}) удалена");

                StatusMessage = $"Статья \"{article.Title}\" успешно удалена.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении статьи");
                StatusMessage = "Произошла ошибка при удалении статьи.";
                return RedirectToPage();
            }
        }

        public bool CanEditArticle(string articleAuthorId)
        {
            if (CurrentUser == null) return false;

            // Автор может редактировать свою статью
            if (CurrentUser.Id == articleAuthorId) return true;

            // Администратор и модератор могут редактировать любые статьи
            return CurrentUserRoles.Contains(Role.Names.Administrator) ||
                   CurrentUserRoles.Contains(Role.Names.Moderator);
        }

        public bool CanDeleteArticle(string articleAuthorId)
        {
            if (CurrentUser == null) return false;

            // Автор может удалить свою статью
            if (CurrentUser.Id == articleAuthorId) return true;

            // Администратор и модератор могут удалить любые статьи
            return CurrentUserRoles.Contains(Role.Names.Administrator) ||
                   CurrentUserRoles.Contains(Role.Names.Moderator);
        }
    }
}