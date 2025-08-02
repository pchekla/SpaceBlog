using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpaceBlog.Pages.Error
{
    public class NotFoundModel : PageModel
    {
        private readonly ILogger<NotFoundModel> _logger;

        public NotFoundModel(ILogger<NotFoundModel> logger)
        {
            _logger = logger;
        }

        public string? RequestedUrl { get; set; }

        public void OnGet()
        {
            RequestedUrl = HttpContext.Request.Path + HttpContext.Request.QueryString;
            
            _logger.LogWarning("Страница не найдена: {Url} - пользователь: {User}", 
                RequestedUrl, 
                User.Identity?.Name ?? "Anonymous");
        }
    }
}
