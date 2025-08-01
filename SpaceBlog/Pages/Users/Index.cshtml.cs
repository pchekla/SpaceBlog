using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Users
{
    [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут управлять пользователями
    public class IndexModel : PageModel
    {
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(UserManager<BlogUser> userManager, ILogger<IndexModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public IList<UserViewModel> Users { get; set; } = new List<UserViewModel>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class UserViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
            public DateTime? LastLoginDate { get; set; }
            public bool IsBanned { get; set; }
            public int ArticlesCount { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            try
            {
                var users = await _userManager.Users
                    .OrderBy(u => u.DisplayName)
                    .ToListAsync();

                var userViewModels = new List<UserViewModel>();

                foreach (var user in users)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    var roleDisplayNames = roles.Select(GetRoleDisplayName).ToList();

                    userViewModels.Add(new UserViewModel
                    {
                        Id = user.Id,
                        DisplayName = user.GetDisplayName(),
                        Email = user.Email ?? "",
                        RegistrationDate = user.RegistrationDate,
                        LastLoginDate = user.LastLoginDate,
                        IsBanned = user.IsBanned,
                        ArticlesCount = user.Articles.Count,
                        Roles = roleDisplayNames
                    });
                }

                Users = userViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка пользователей");
                Users = new List<UserViewModel>();
            }
        }

        public async Task<IActionResult> OnPostBanAsync(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    StatusMessage = "Пользователь не найден.";
                    return RedirectToPage();
                }

                user.IsBanned = true;
                user.BanReason = "Заблокирован администратором";
                // BannedAt свойство устанавливается автоматически при IsBanned = true

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Пользователь {user.DisplayName} (ID: {user.Id}) заблокирован");
                    StatusMessage = $"Пользователь \"{user.GetDisplayName()}\" успешно заблокирован.";
                }
                else
                {
                    StatusMessage = $"Ошибка при блокировке пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при блокировке пользователя");
                StatusMessage = "Произошла ошибка при блокировке пользователя.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUnbanAsync(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    StatusMessage = "Пользователь не найден.";
                    return RedirectToPage();
                }

                user.IsBanned = false;
                user.BanReason = null;
                // BannedAt сбрасывается автоматически при IsBanned = false

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Пользователь {user.DisplayName} (ID: {user.Id}) разблокирован");
                    StatusMessage = $"Пользователь \"{user.GetDisplayName()}\" успешно разблокирован.";
                }
                else
                {
                    StatusMessage = $"Ошибка при разблокировке пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при разблокировке пользователя");
                StatusMessage = "Произошла ошибка при разблокировке пользователя.";
            }

            return RedirectToPage();
        }

        private static string GetRoleDisplayName(string roleName)
        {
            return roleName switch
            {
                Role.Names.Administrator => Role.DisplayNames.Administrator,
                Role.Names.Moderator => Role.DisplayNames.Moderator,
                Role.Names.Author => Role.DisplayNames.Author,
                Role.Names.User => Role.DisplayNames.User,
                _ => roleName
            };
        }
    }
}