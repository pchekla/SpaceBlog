using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Articles
{
    [Authorize] // Авторизованные пользователи могут редактировать статьи
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public List<int> SelectedTagIds { get; set; } = new();

        public Article Article { get; set; } = null!;
        public List<Tag> AvailableTags { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Заголовок статьи обязателен для заполнения")]
            [StringLength(200, MinimumLength = 5, ErrorMessage = "Заголовок должен содержать от {2} до {1} символов")]
            [Display(Name = "Заголовок")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "Контент статьи обязателен для заполнения")]
            [StringLength(10000, MinimumLength = 50, ErrorMessage = "Контент должен содержать от {2} до {1} символов")]
            [Display(Name = "Контент")]
            public string Content { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            Article = article;

            // Проверяем, что пользователь может редактировать эту статью
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool canEdit = Article.AuthorId == currentUser.Id || 
                          userRoles.Contains(Role.Names.Administrator) ||
                          userRoles.Contains(Role.Names.Moderator);

            if (!canEdit)
            {
                return Forbid();
            }

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var article = await _context.Articles
                .Include(a => a.Author)
                .Include(a => a.ArticleTags)
                    .ThenInclude(at => at.Tag)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null)
            {
                return NotFound();
            }

            Article = article;

            // Проверяем права доступа
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userRoles = await _userManager.GetRolesAsync(currentUser);
            bool canEdit = Article.AuthorId == currentUser.Id || 
                          userRoles.Contains(Role.Names.Administrator) ||
                          userRoles.Contains(Role.Names.Moderator);

            if (!canEdit)
            {
                return Forbid();
            }

            await LoadTagsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Обновляем статью
                Article.Title = Input.Title.Trim();
                Article.Content = Input.Content.Trim();
                Article.UpdatedAt = DateTime.Now;
                Article.GenerateSlug();

                // Удаляем старые связи с тегами
                var existingArticleTags = await _context.ArticleTags
                    .Where(at => at.ArticleId == Article.Id)
                    .ToListAsync();
                
                _context.ArticleTags.RemoveRange(existingArticleTags);

                // Добавляем новые связи с тегами
                foreach (var tagId in SelectedTagIds)
                {
                    var articleTag = new ArticleTag
                    {
                        ArticleId = Article.Id,
                        TagId = tagId
                    };
                    _context.ArticleTags.Add(articleTag);
                }

                _context.Articles.Update(Article);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Статья '{Article.Title}' (ID: {Article.Id}) обновлена пользователем {currentUser.DisplayName}");

                StatusMessage = $"Статья \"{Article.Title}\" успешно обновлена!";
                return RedirectToPage("./Details", new { id = Article.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении статьи");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении статьи. Попробуйте еще раз.");
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            // Загружаем данные в форму
            Input.Title = Article.Title;
            Input.Content = Article.Content;

            // Загружаем доступные теги
            AvailableTags = await _context.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Загружаем выбранные теги
            SelectedTagIds = Article.ArticleTags
                .Select(at => at.TagId)
                .ToList();
        }

        private async Task LoadTagsAsync()
        {
            // Загружаем только доступные теги (без перезаписи Input данных)
            AvailableTags = await _context.Tags
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Загружаем выбранные теги из текущей статьи
            SelectedTagIds = Article.ArticleTags
                .Select(at => at.TagId)
                .ToList();
        }
    }
}