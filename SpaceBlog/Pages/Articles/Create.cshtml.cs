using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Articles
{
    [Authorize] // Требуется авторизация для создания статей
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public List<int> SelectedTagIds { get; set; } = new();

        public List<Tag> AvailableTags { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Заголовок статьи обязателен для заполнения")]
            [StringLength(200, MinimumLength = 5, ErrorMessage = "Заголовок должен содержать от {2} до {1} символов")]
            [Display(Name = "Заголовок")]
            public string Title { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "Краткое содержание не должно превышать {1} символов")]
            [Display(Name = "Краткое содержание")]
            public string? Summary { get; set; }

            [Required(ErrorMessage = "Содержание статьи обязательно для заполнения")]
            [StringLength(10000, MinimumLength = 50, ErrorMessage = "Содержание должно содержать от {2} до {1} символов")]
            [Display(Name = "Контент")]
            public string Content { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadAvailableTagsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadAvailableTagsAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    ModelState.AddModelError(string.Empty, "Пользователь не найден");
                    return Page();
                }

                // Создаем новую статью
                var article = new Article
                {
                    Title = Input.Title.Trim(),
                    Summary = string.IsNullOrWhiteSpace(Input.Summary) ? null : Input.Summary.Trim(),
                    Content = Input.Content.Trim(),
                    AuthorId = currentUser.Id,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsPublished = true,
                    PublishedAt = DateTime.Now
                };

                // Генерируем slug
                article.GenerateSlug();

                _context.Articles.Add(article);
                await _context.SaveChangesAsync();

                // Добавляем связи с тегами
                if (SelectedTagIds.Any())
                {
                    var articleTags = SelectedTagIds.Select(tagId => new ArticleTag
                    {
                        ArticleId = article.Id,
                        TagId = tagId
                    }).ToList();

                    _context.ArticleTags.AddRange(articleTags);
                    await _context.SaveChangesAsync();
                }

                // Счетчик статей обновится автоматически через вычисляемое свойство

                _logger.LogInformation($"Создана новая статья: {article.Title} (ID: {article.Id}) пользователем {currentUser.DisplayName}");

                StatusMessage = $"Статья \"{article.Title}\" успешно создана!";
                
                // Перенаправляем на список всех статей (доступен всем пользователям)
                return RedirectToPage("/Articles/All");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании статьи");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при создании статьи. Попробуйте еще раз.");
                return Page();
            }
        }

        private async Task LoadAvailableTagsAsync()
        {
            try
            {
                AvailableTags = await _context.Tags
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке тегов");
                AvailableTags = new List<Tag>();
            }
        }
    }
}
