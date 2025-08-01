using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

namespace SpaceBlog.Pages.Users
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, UserManager<BlogUser> userManager, ILogger<DetailsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public UserProfileViewModel? UserProfile { get; set; }
        public IList<UserArticleViewModel> UserArticles { get; set; } = new List<UserArticleViewModel>();
        public IList<UserCommentViewModel> UserComments { get; set; } = new List<UserCommentViewModel>();
        public BlogUser? CurrentUser { get; set; }
        public IList<string> CurrentUserRoles { get; set; } = new List<string>();
        public string? CurrentUserId { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        public class UserProfileViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public DateTime RegistrationDate { get; set; }
            public DateTime? LastLoginDate { get; set; }
            public bool IsOnline { get; set; }
            public int ArticlesCount { get; set; }
            public int PublishedArticles { get; set; }
            public int DraftArticles { get; set; }
            public int CommentsCount { get; set; }
            public int ApprovedComments { get; set; }
            public int TotalViews { get; set; }
            public List<string> Roles { get; set; } = new();
        }

        public class UserArticleViewModel
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string? Summary { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool IsPublished { get; set; }
            public int ViewCount { get; set; }
        }

        public class UserCommentViewModel
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public bool IsApproved { get; set; }
            public int ArticleId { get; set; }
            public string ArticleTitle { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string id)
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

                // Загружаем пользователя
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound();
                }

                // Загружаем статьи пользователя
                var userArticles = await _context.Articles
                    .Where(a => a.AuthorId == id)
                    .OrderByDescending(a => a.CreatedAt)
                    .Select(a => new UserArticleViewModel
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Summary = a.Summary,
                        CreatedAt = a.CreatedAt,
                        IsPublished = a.IsPublished,
                        ViewCount = a.ViewCount
                    })
                    .ToListAsync();

                // Загружаем комментарии пользователя
                var userComments = await _context.Comments
                    .Include(c => c.Article)
                    .Where(c => c.AuthorId == id)
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new UserCommentViewModel
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        IsApproved = c.IsApproved,
                        ArticleId = c.Article != null ? c.Article.Id : 0,
                        ArticleTitle = c.Article != null ? c.Article.Title : "Удаленная статья"
                    })
                    .ToListAsync();

                // Получаем роли пользователя
                var userRoles = await _userManager.GetRolesAsync(user);

                // Вычисляем статистику
                var publishedArticles = userArticles.Count(a => a.IsPublished);
                var draftArticles = userArticles.Count(a => !a.IsPublished);
                var totalViews = userArticles.Sum(a => a.ViewCount);
                var approvedComments = userComments.Count(c => c.IsApproved);

                // Определяем онлайн статус (упрощенно - последний вход менее часа назад)
                var isOnline = user.LastLoginDate.HasValue && 
                              (DateTime.UtcNow - user.LastLoginDate.Value).TotalHours < 1;

                UserProfile = new UserProfileViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.GetDisplayName(),
                    RegistrationDate = user.RegistrationDate,
                    LastLoginDate = user.LastLoginDate,
                    IsOnline = isOnline,
                    ArticlesCount = userArticles.Count,
                    PublishedArticles = publishedArticles,
                    DraftArticles = draftArticles,
                    CommentsCount = userComments.Count,
                    ApprovedComments = approvedComments,
                    TotalViews = totalViews,
                    Roles = userRoles.ToList()
                };

                UserArticles = userArticles;
                UserComments = userComments;

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке профиля пользователя с ID: {UserId}", id);
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostAsync(string userId, string actionType)
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
                    return RedirectToPage(new { id = userId });
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

                return RedirectToPage(new { id = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выполнении действия над пользователем");
                StatusMessage = "Произошла ошибка при выполнении действия.";
                return RedirectToPage(new { id = userId });
            }
        }

        public bool CanManageUsers()
        {
            if (CurrentUser == null) return false;

            // Администратор может управлять пользователями
            return CurrentUserRoles.Contains(Role.Names.Administrator);
        }
    }
}