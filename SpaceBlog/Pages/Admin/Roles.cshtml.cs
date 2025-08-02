using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Admin
{
    [Authorize(Roles = Role.Names.Administrator)]
    public class RolesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<RolesModel> _logger;

        public RolesModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<RolesModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<UserRoleViewModel> Users { get; set; } = new List<UserRoleViewModel>();
        public RoleStatisticsViewModel RoleStats { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public class UserRoleViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        public class RoleStatisticsViewModel
        {
            public int AdministratorsCount { get; set; }
            public int ModeratorsCount { get; set; }
            public int AuthorsCount { get; set; }
            public int UsersCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Загружаем всех пользователей
                var usersQuery = _context.Users.AsQueryable();

                // Применяем фильтры поиска
                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    var searchLower = SearchQuery.ToLower();
                    usersQuery = usersQuery.Where(u => 
                        (u.UserName != null && u.UserName.ToLower().Contains(searchLower)) ||
                        (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                        (u.FirstName != null && u.FirstName.ToLower().Contains(searchLower)) ||
                        (u.LastName != null && u.LastName.ToLower().Contains(searchLower)));
                }

                var users = await usersQuery
                    .OrderBy(u => u.UserName)
                    .ToListAsync();

                var userViewModels = new List<UserRoleViewModel>();
                int adminCount = 0, modCount = 0, authorCount = 0, userCount = 0;

                foreach (var user in users)
                {
                    // Получаем роли пользователя
                    var userRoles = await _userManager.GetRolesAsync(user);

                    userViewModels.Add(new UserRoleViewModel
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        DisplayName = user.GetDisplayName(),
                        RegistrationDate = user.RegistrationDate,
                        Roles = userRoles.ToList()
                    });

                    // Подсчитываем статистику
                    if (userRoles.Contains(Role.Names.Administrator)) adminCount++;
                    if (userRoles.Contains(Role.Names.Moderator)) modCount++;
                    if (userRoles.Contains(Role.Names.Author)) authorCount++;
                    if (userRoles.Contains(Role.Names.User)) userCount++;
                }

                Users = userViewModels;
                RoleStats = new RoleStatisticsViewModel
                {
                    AdministratorsCount = adminCount,
                    ModeratorsCount = modCount,
                    AuthorsCount = authorCount,
                    UsersCount = userCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке пользователей для управления ролями");
                Users = new List<UserRoleViewModel>();
                StatusMessage = "Произошла ошибка при загрузке пользователей.";
            }
        }

        public async Task<IActionResult> OnPostChangeRolesAsync(string userId, string selectedRole)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    StatusMessage = "Пользователь не найден.";
                    return RedirectToPage();
                }

                // Получаем текущие роли пользователя
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Удаляем все текущие роли
                if (currentRoles.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        StatusMessage = "Ошибка при удалении текущих ролей.";
                        return RedirectToPage();
                    }
                }

                // Добавляем новую роль
                if (!string.IsNullOrEmpty(selectedRole) && 
                    (selectedRole == Role.Names.Administrator || 
                     selectedRole == Role.Names.Moderator || 
                     selectedRole == Role.Names.Author || 
                     selectedRole == Role.Names.User))
                {
                    var addResult = await _userManager.AddToRoleAsync(user, selectedRole);
                    if (!addResult.Succeeded)
                    {
                        StatusMessage = "Ошибка при назначении новой роли.";
                        return RedirectToPage();
                    }
                }

                _logger.LogInformation("Роль пользователя {UserId} изменена администратором {AdminId}. Новая роль: {Role}", 
                    userId, User.Identity?.Name, selectedRole ?? "Без роли");

                StatusMessage = $"Роли пользователя {user.GetDisplayName()} успешно обновлены.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении ролей пользователя");
                StatusMessage = "Произошла ошибка при изменении ролей.";
                return RedirectToPage();
            }
        }
    }
}