using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;

namespace SpaceBlog.Pages.Tags
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

        public IList<Tag> MyTags { get; set; } = new List<Tag>();

        public async Task OnGetAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                _logger.LogWarning("Пользователь не найден при загрузке страницы 'Мои теги'");
                MyTags = new List<Tag>();
                return;
            }

            try
            {
                MyTags = await _context.Tags
                    .Include(t => t.CreatedBy)
                    .Include(t => t.ArticleTags)
                    .Where(t => t.CreatedById == currentUser.Id)
                    .ToListAsync();

                _logger.LogInformation("Загружено {Count} тегов для пользователя {UserId}", 
                    MyTags.Count, currentUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке тегов пользователя {UserId}", currentUser.Id);
                MyTags = new List<Tag>();
            }
        }
    }
}
