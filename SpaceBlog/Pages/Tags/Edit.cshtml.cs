using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Tags
{
    [Authorize] // Авторизованные пользователи могут редактировать теги
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public Tag Tag { get; set; } = null!;

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Название тега обязательно для заполнения")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Название должно содержать от {2} до {1} символов")]
            [Display(Name = "Название")]
            public string Name { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.ArticleTags)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
            {
                return NotFound();
            }

            Tag = tag;

            // Загружаем данные в форму
            Input.Name = Tag.Name;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (id <= 0)
            {
                return NotFound();
            }

            var tag = await _context.Tags
                .Include(t => t.ArticleTags)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tag == null)
            {
                return NotFound();
            }

            Tag = tag;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Проверяем уникальность названия
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == Input.Name.Trim().ToLower() && t.Id != id);

                if (existingTag != null)
                {
                    ModelState.AddModelError(nameof(Input.Name), "Тег с таким названием уже существует.");
                    return Page();
                }

                // Обновляем тег
                Tag.Name = Input.Name.Trim();
                Tag.GenerateSlug();

                _context.Tags.Update(Tag);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Тег '{Tag.Name}' (ID: {Tag.Id}) обновлен");

                StatusMessage = $"Тег \"{Tag.Name}\" успешно обновлен!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении тега");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении тега. Попробуйте еще раз.");
                return Page();
            }
        }
    }
}