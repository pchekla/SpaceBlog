using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Roles
{
    [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут создавать роли
    public class CreateModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(RoleManager<Role> roleManager, ILogger<CreateModel> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Название роли обязательно для заполнения")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Название роли должно содержать от {2} до {1} символов")]
            [Display(Name = "Название")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "Описание роли обязательно для заполнения")]
            [StringLength(500, MinimumLength = 10, ErrorMessage = "Описание должно содержать от {2} до {1} символов")]
            [Display(Name = "Описание")]
            public string Description { get; set; } = string.Empty;
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
                // Проверяем, не существует ли уже роль с таким названием
                if (await _roleManager.RoleExistsAsync(Input.Name))
                {
                    ModelState.AddModelError("Input.Name", "Роль с таким названием уже существует");
                    return Page();
                }

                // Создаем новую роль
                var role = new Role
                {
                    Name = Input.Name.Trim(),
                    Description = Input.Description.Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    Priority = 0 // По умолчанию низкий приоритет
                };

                var result = await _roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Создана новая роль: {role.Name} (ID: {role.Id})");
                    StatusMessage = $"Роль \"{role.Name}\" успешно создана!";
                    
                    // Перенаправляем на список ролей
                    return RedirectToPage("./Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании роли");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при создании роли. Попробуйте еще раз.");
                return Page();
            }
        }
    }
}
