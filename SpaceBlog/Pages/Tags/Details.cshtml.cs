using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;

namespace SpaceBlog.Pages.Tags
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Tag Tag { get; set; } = null!;
        public IList<ArticleDisplayViewModel> Articles { get; set; } = new List<ArticleDisplayViewModel>();

        public class ArticleDisplayViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public string AuthorName { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsPublished { get; set; }
            public int ViewCount { get; set; }
            public int CommentsCount { get; set; }
            public List<string> Tags { get; set; } = new();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            try
            {
                            var tag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
                {
                    return NotFound();
                }

                Tag = tag;

                // Увеличиваем счетчик просмотров тега
                Tag.ViewCount++;
                _context.Tags.Update(Tag);
                await _context.SaveChangesAsync();

                // Загружаем статьи с этим тегом
                var articles = await _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Include(a => a.Comments)
                    .Where(a => a.ArticleTags.Any(at => at.TagId == id) && a.IsPublished)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                var articleViewModels = new List<ArticleDisplayViewModel>();

                foreach (var article in articles)
                {
                    var tags = article.ArticleTags
                        .Where(at => at.Tag != null)
                        .Select(at => at.Tag!.Name)
                        .ToList();

                    articleViewModels.Add(new ArticleDisplayViewModel
                    {
                        Id = article.Id,
                        Title = article.Title,
                        Content = article.Content,
                        Summary = article.Summary,
                        AuthorName = article.Author?.GetDisplayName() ?? "Неизвестный автор",
                        CreatedAt = article.CreatedAt,
                        IsPublished = article.IsPublished,
                        ViewCount = article.ViewCount,
                        CommentsCount = article.Comments.Count(c => c.IsApproved),
                        Tags = tags
                    });
                }

                Articles = articleViewModels;
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке деталей тега с ID {TagId}", id);
                return NotFound();
            }
        }
    }
}
