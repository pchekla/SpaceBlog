using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Models;

namespace SpaceBlog.Pages.Roles
{
    [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут просматривать все роли
    public class AllModel : PageModel
    {
        private readonly RoleManager<Role> _roleManager;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<AllModel> _logger;

        public AllModel(RoleManager<Role> roleManager, UserManager<BlogUser> userManager, ILogger<AllModel> logger)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<RoleDisplayViewModel> Roles { get; set; } = new List<RoleDisplayViewModel>();

        [TempData]
        public string? StatusMessage { get; set; }

        public class RoleDisplayViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? Description { get; set; }
            public DateTime CreatedAt { get; set; }
            public int Priority { get; set; }
            public bool IsActive { get; set; }
            public bool IsSystemRole { get; set; }
            public int UserCount { get; set; }
            public List<string> RecentUsers { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .OrderByDescending(r => r.Priority)
                    .ThenBy(r => r.Name)
                    .ToListAsync();

                var roleViewModels = new List<RoleDisplayViewModel>();

                foreach (var role in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name ?? "");
                    var recentUsers = usersInRole
                        .OrderBy(u => u.DisplayName)
                        .Take(5)
                        .Select(u => u.GetDisplayName())
                        .ToList();

                    roleViewModels.Add(new RoleDisplayViewModel
                    {
                        Id = role.Id,
                        Name = role.Name ?? "",
                        DisplayName = GetRoleDisplayName(role.Name ?? ""),
                        Description = role.Description,
                        CreatedAt = role.CreatedAt,
                        Priority = role.Priority,
                        IsActive = role.IsActive,
                        IsSystemRole = IsSystemRole(role.Name ?? ""),
                        UserCount = usersInRole.Count,
                        RecentUsers = recentUsers
                    });
                }

                Roles = roleViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка всех ролей");
                Roles = new List<RoleDisplayViewModel>();
                StatusMessage = "Произошла ошибка при загрузке ролей.";
            }
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

        private static bool IsSystemRole(string roleName)
        {
            return roleName == Role.Names.Administrator ||
                   roleName == Role.Names.Moderator ||
                   roleName == Role.Names.Author ||
                   roleName == Role.Names.User;
        }
    }
}
