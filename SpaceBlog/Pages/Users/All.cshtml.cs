using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;

namespace SpaceBlog.Pages.Users
{
    public class AllModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<AllModel> _logger;

        public AllModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<AllModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public IList<UserListViewModel> Users { get; set; } = new List<UserListViewModel>();
        public BlogUser? CurrentUser { get; set; }
        public IList<string> CurrentUserRoles { get; set; } = new List<string>();
        public string? CurrentUserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? RoleFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest";

        [TempData]
        public string? StatusMessage { get; set; }

        public class UserListViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
            public DateTime? LastLoginDate { get; set; }
            public bool IsOnline { get; set; }
            public int ArticlesCount { get; set; }
            public int CommentsCount { get; set; }
            public int TotalViews { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        public async Task OnGetAsync()
        {
            try
            {
                // Загружаем текущего пользователя и его роли
                if (User.Identity != null && User.Identity.IsAuthenticated)
                {
                    CurrentUser = await _userManager.GetUserAsync(User);
                    if (CurrentUser != null)
                    {
                        CurrentUserId = CurrentUser.Id;
                        CurrentUserRoles = await _userManager.GetRolesAsync(CurrentUser);
                    }
                }

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

                // Применяем сортировку
                usersQuery = SortBy switch
                {
                    "oldest" => usersQuery.OrderBy(u => u.RegistrationDate),
                    "active" => usersQuery.OrderByDescending(u => u.LastLoginDate),
                    "articles" => usersQuery.OrderByDescending(u => u.Articles.Count),
                    _ => usersQuery.OrderByDescending(u => u.RegistrationDate)
                };

                var users = await usersQuery.ToListAsync();
                var userViewModels = new List<UserListViewModel>();

                foreach (var user in users)
                {
                    // Получаем роли пользователя
                    var userRoles = await _userManager.GetRolesAsync(user);

                    // Фильтр по ролям
                    if (!string.IsNullOrEmpty(RoleFilter) && !userRoles.Contains(RoleFilter))
                    {
                        continue;
                    }

                    // Получаем статистику статей
                    var articles = await _context.Articles
                        .Where(a => a.AuthorId == user.Id)
                        .ToListAsync();

                    var articlesCount = articles.Count;
                    var totalViews = articles.Sum(a => a.ViewCount);

                    // Получаем количество комментариев
                    var commentsCount = await _context.Comments
                        .CountAsync(c => c.AuthorId == user.Id);

                    // Определяем онлайн статус (упрощенно - последний вход менее часа назад)
                    var isOnline = user.LastLoginDate.HasValue && 
                                  (DateTime.UtcNow - user.LastLoginDate.Value).TotalHours < 1;

                    userViewModels.Add(new UserListViewModel
                    {
                        Id = user.Id,
                        UserName = user.UserName ?? string.Empty,
                        Email = user.Email ?? string.Empty,
                        DisplayName = user.GetDisplayName(),
                        RegistrationDate = user.RegistrationDate,
                        LastLoginDate = user.LastLoginDate,
                        IsOnline = isOnline,
                        ArticlesCount = articlesCount,
                        CommentsCount = commentsCount,
                        TotalViews = totalViews,
                        Roles = userRoles.ToList()
                    });
                }

                Users = userViewModels;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка пользователей");
                Users = new List<UserListViewModel>();
                StatusMessage = "Произошла ошибка при загрузке пользователей.";
            }
        }

        public async Task<IActionResult> OnPostActionAsync(string userId, string actionType)
        {
            try
            {
                if (!CanManageUsers())
                {
                    return Forbid();
                }

                var targetUser = await _context.Users.FindAsync(userId);
                if (targetUser == null)
                {
                    StatusMessage = "Пользователь не найден.";
                    return RedirectToPage();
                }

                switch (actionType)
                {
                    case "ban":
                        // Здесь можно реализовать блокировку пользователя
                        // Например, добавить поле IsBlocked в модель BlogUser
                        StatusMessage = $"Пользователь {targetUser.GetDisplayName()} заблокирован.";
                        _logger.LogInformation("Пользователь {UserId} заблокирован администратором {AdminId}", 
                            userId, CurrentUserId);
                        break;
                    default:
                        StatusMessage = "Неизвестное действие.";
                        break;
                }

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении действия над пользователем");
                StatusMessage = "Произошла ошибка при выполнении действия.";
                return RedirectToPage();
            }
        }

        public bool CanManageUsers()
        {
            if (CurrentUser == null) return false;

            // Администратор и модератор могут управлять пользователями
            return CurrentUserRoles.Contains(Role.Names.Administrator) || 
                   CurrentUserRoles.Contains(Role.Names.Moderator);
        }
    }
}
