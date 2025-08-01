using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Roles
{
    [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут управлять ролями
    public class IndexModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(RoleManager<Role> roleManager, ILogger<IndexModel> logger)
        {
            _roleManager = roleManager;
            _logger = logger;
        }

        public IList<RoleViewModel> Roles { get; set; } = new List<RoleViewModel>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class RoleViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Priority { get; set; }
            public int UserCount { get; set; }
            public bool IsSystemRole { get; set; }
        }

        public async Task OnGetAsync()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Include(r => r.UserRoles)
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.Name)
                    .ToListAsync();

                Roles = roles.Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name ?? "",
                    Description = r.Description,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt,
                    Priority = r.Priority,
                    UserCount = r.UserRoles.Count,
                    IsSystemRole = IsSystemRole(r.Name ?? "")
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка ролей");
                Roles = new List<RoleViewModel>();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(id);
                if (role == null)
                {
                    StatusMessage = "Роль не найдена.";
                    return RedirectToPage();
                }

                // Проверяем, является ли роль системной
                if (IsSystemRole(role.Name ?? ""))
                {
                    StatusMessage = $"Роль \"{role.Name}\" нельзя удалить, так как она является системной.";
                    return RedirectToPage();
                }

                // Проверяем, используется ли роль
                var userCount = (await _roleManager.Roles
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.Id == id))?.UserRoles.Count ?? 0;

                if (userCount > 0)
                {
                    StatusMessage = $"Роль \"{role.Name}\" нельзя удалить, так как она назначена {userCount} пользователям.";
                    return RedirectToPage();
                }

                var result = await _roleManager.DeleteAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Удалена роль: {role.Name} (ID: {role.Id})");
                    StatusMessage = $"Роль \"{role.Name}\" успешно удалена.";
                }
                else
                {
                    StatusMessage = $"Ошибка при удалении роли: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении роли");
                StatusMessage = "Произошла ошибка при удалении роли.";
            }

            return RedirectToPage();
        }

        private static bool IsSystemRole(string roleName)
        {
            return roleName == Role.Names.Administrator ||
                   roleName == Role.Names.Moderator ||
                   roleName == Role.Names.Author ||
                   roleName == Role.Names.User;
        }
    }
}