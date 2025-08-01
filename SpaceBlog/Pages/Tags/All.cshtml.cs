using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Tags
{
    public class AllModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AllModel> _logger;

        public AllModel(ApplicationDbContext context, ILogger<AllModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<TagDisplayViewModel> Tags { get; set; } = new List<TagDisplayViewModel>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class TagDisplayViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public int ViewCount { get; set; }
            public int ArticleCount { get; set; }
            public List<string> RecentArticles { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            try
            {
                var tags = await _context.Tags
                    .Include(t => t.ArticleTags)
                        .ThenInclude(at => at.Article)
                    .OrderByDescending(t => t.ArticleTags.Count)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                var tagViewModels = new List<TagDisplayViewModel>();

                foreach (var tag in tags)
                {
                    var recentArticles = tag.ArticleTags
                        .Where(at => at.Article != null && at.Article.IsPublished)
                        .OrderByDescending(at => at.Article!.CreatedAt)
                        .Take(3)
                        .Select(at => at.Article!.Title)
                        .ToList();

                    tagViewModels.Add(new TagDisplayViewModel
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        Slug = tag.Slug ?? string.Empty,
                        CreatedAt = tag.CreatedAt,
                        ViewCount = tag.ViewCount,
                        ArticleCount = tag.ArticleTags.Count(at => at.Article != null && at.Article!.IsPublished),
                        RecentArticles = recentArticles
                    });
                }

                Tags = tagViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка всех тегов");
                Tags = new List<TagDisplayViewModel>();
                StatusMessage = "Произошла ошибка при загрузке тегов.";
            }
        }
    }
}