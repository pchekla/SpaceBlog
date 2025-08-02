using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Roles
{
    [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут редактировать роли
    public class EditModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(RoleManager<Role> roleManager, UserManager<BlogUser> userManager, ILogger<EditModel> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public RoleEditViewModel Role { get; set; } = null!;

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Название роли обязательно для заполнения")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Название должно содержать от {2} до {1} символов")]
            [Display(Name = "Название")]
            public string Name { get; set; } = string.Empty;

            [StringLength(500, ErrorMessage = "Описание не должно превышать {1} символов")]
            [Display(Name = "Описание")]
            public string? Description { get; set; }
        }

        public class RoleEditViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int UserCount { get; set; }
            public bool IsSystemRole { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            // Загружаем информацию о роли
            await LoadRoleDataAsync(role);

            // Загружаем данные в форму
            Input.Name = role.Name ?? "";
            Input.Description = role.Description;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null)
            {
                return NotFound();
            }

            await LoadRoleDataAsync(role);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Проверяем, что это не системная роль
            if (Role.IsSystemRole && role.Name != Input.Name.Trim())
            {
                ModelState.AddModelError(nameof(Input.Name), "Нельзя изменять название системных ролей.");
                return Page();
            }

            try
            {
                // Проверяем уникальность названия
                if (role.Name != Input.Name.Trim())
                {
                    var existingRole = await _roleManager.FindByNameAsync(Input.Name.Trim());
                    if (existingRole != null)
                    {
                        ModelState.AddModelError(nameof(Input.Name), "Роль с таким названием уже существует.");
                        return Page();
                    }
                }

                // Обновляем роль
                role.Name = Input.Name.Trim();
                role.Description = Input.Description?.Trim();

                var result = await _roleManager.UpdateAsync(role);
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }

                _logger.LogInformation($"Роль '{role.Name}' (ID: {role.Id}) обновлена");

                StatusMessage = $"Роль \"{role.Name}\" успешно обновлена!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении роли");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении роли. Попробуйте еще раз.");
                return Page();
            }
        }

        private async Task LoadRoleDataAsync(Role role)
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? "");
            
            Role = new RoleEditViewModel
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Description = role.Description,
                UserCount = usersInRole.Count,
                IsSystemRole = IsSystemRole(role.Name ?? "")
            };
        }

        private static bool IsSystemRole(string roleName)
        {
            return roleName == SpaceBlog.Api.Models.Role.Names.Administrator ||
                   roleName == SpaceBlog.Api.Models.Role.Names.Moderator ||
                   roleName == SpaceBlog.Api.Models.Role.Names.Author ||
                   roleName == SpaceBlog.Api.Models.Role.Names.User;
        }
    }
}
