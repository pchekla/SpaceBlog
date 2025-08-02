using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Articles
{
    [Authorize]
    public class MyModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<MyModel> _logger;

        public MyModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<MyModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<Article> MyArticles { get; set; } = new List<Article>();

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Пользователь не найден при загрузке страницы 'Мои статьи'");
                MyArticles = new List<Article>();
                return;
            }

            try
            {
                MyArticles = await _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.Comments)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Where(a => a.AuthorId == currentUser.Id)
                    .ToListAsync();

                _logger.LogInformation("Загружено {Count} статей для пользователя {UserId}", 
                    MyArticles.Count, currentUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке статей пользователя {UserId}", currentUser.Id);
                MyArticles = new List<Article>();
            }
        }
    }
}