using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Tags
{
    [Authorize] // Требуется авторизация для создания тегов
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

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Название тега обязательно для заполнения")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Название тега должно содержать от {2} до {1} символов")]
            [Display(Name = "Название")]
            public string Name { get; set; } = string.Empty;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Проверяем, не существует ли уже тег с таким названием
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == Input.Name.ToLower());

                if (existingTag != null)
                {
                    ModelState.AddModelError("Input.Name", "Тег с таким названием уже существует");
                    return Page();
                }

                // Получаем текущего пользователя
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Создаем новый тег
                var tag = new Tag
                {
                    Name = Input.Name.Trim(),
                    Description = null,
                    Color = "#6c757d",
                    CreatedAt = DateTime.Now,
                    CreatedById = currentUser?.Id,
                    IsActive = true,
                    SortOrder = 0,
                    ViewCount = 0
                };
                
                // Генерируем slug
                tag.Slug = tag.GenerateSlug();

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Создан новый тег: {tag.Name} (ID: {tag.Id})");

                StatusMessage = $"Тег \"{tag.Name}\" успешно создан!";
                
                // Перенаправляем на мои теги
                return RedirectToPage("./My");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании тега");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при создании тега. Попробуйте еще раз.");
                return Page();
            }
        }
    }
}
