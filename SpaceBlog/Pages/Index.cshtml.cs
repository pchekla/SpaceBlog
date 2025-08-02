using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;

namespace SpaceBlog.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public int TotalArticles { get; set; }
        public int TotalUsers { get; set; }
        public int TotalComments { get; set; }
        public int TotalTags { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                TotalArticles = await _context.Articles.Where(a => a.IsPublished).CountAsync();
                TotalUsers = await _context.Users.CountAsync();
                TotalComments = await _context.Comments.CountAsync();
                TotalTags = await _context.Tags.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке статистики на главной странице");
                // Установим значения по умолчанию в случае ошибки
                TotalArticles = 0;
                TotalUsers = 0;
                TotalComments = 0;
                TotalTags = 0;
            }
        }
    }
}
