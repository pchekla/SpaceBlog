using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpaceBlog.Pages.Error
{
    public class ForbiddenModel : PageModel
    {
        private readonly ILogger<ForbiddenModel> _logger;

        public ForbiddenModel(ILogger<ForbiddenModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogWarning("Доступ запрещен для пользователя: {User} к URL: {Url}", 
                User.Identity?.Name ?? "Anonymous", 
                HttpContext.Request.Path);
        }
    }
}