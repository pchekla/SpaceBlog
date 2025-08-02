using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Tags
{
    [Authorize(Policy = "RequireModerator")] // Требуется роль модератора или администратора для управления тегами
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IList<Tag> Tags { get; set; } = new List<Tag>();

        [TempData]
        public string? StatusMessage { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                Tags = await _context.Tags
                    .Include(t => t.ArticleTags)
                    .OrderBy(t => t.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка тегов");
                Tags = new List<Tag>();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    StatusMessage = "Тег не найден.";
                    return RedirectToPage();
                }

                // Проверяем, используется ли тег в статьях
                var usageCount = await _context.ArticleTags
                    .CountAsync(at => at.TagId == tag.Id);

                if (usageCount > 0)
                {
                    StatusMessage = $"Тег \"{tag.Name}\" нельзя удалить, так как он используется в {usageCount} статьях.";
                    return RedirectToPage();
                }

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Удален тег: {tag.Name} (ID: {tag.Id})");
                StatusMessage = $"Тег \"{tag.Name}\" успешно удален.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении тега");
                StatusMessage = "Произошла ошибка при удалении тега.";
            }

            return RedirectToPage();
        }
    }
}