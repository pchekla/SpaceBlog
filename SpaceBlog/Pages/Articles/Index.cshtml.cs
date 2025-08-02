using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Articles
{
    [Authorize(Policy = "RequireModerator")] // Требуется роль модератора или администратора
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<ArticleViewModel> Articles { get; set; } = new List<ArticleViewModel>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class ArticleViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public string Content { get; set; } = string.Empty;
            public string AuthorName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsPublished { get; set; }
            public int ViewsCount { get; set; }
            public List<string> Tags { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            try
            {
                var articles = await _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                Articles = articles.Select(a => new ArticleViewModel
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    Content = a.Content,
                    AuthorName = a.Author?.DisplayName ?? "Неизвестный автор",
                    CreatedAt = a.CreatedAt,
                    IsPublished = a.IsPublished,
                    ViewsCount = a.ViewCount,
                    Tags = a.ArticleTags.Select(at => at.Tag?.Name ?? "").Where(t => !string.IsNullOrEmpty(t)).ToList()
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка статей");
                Articles = new List<ArticleViewModel>();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var article = await _context.Articles
                    .Include(a => a.ArticleTags)
                    .Include(a => a.Comments)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null)
                {
                    StatusMessage = "Статья не найдена.";
                    return RedirectToPage();
                }

                // Удаляем связи с тегами
                _context.ArticleTags.RemoveRange(article.ArticleTags);

                // Удаляем комментарии
                _context.Comments.RemoveRange(article.Comments);

                // Удаляем статью
                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Удалена статья: {article.Title} (ID: {article.Id})");
                StatusMessage = $"Статья \"{article.Title}\" успешно удалена.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении статьи");
                StatusMessage = "Произошла ошибка при удалении статьи.";
            }

            return RedirectToPage();
        }
    }
}