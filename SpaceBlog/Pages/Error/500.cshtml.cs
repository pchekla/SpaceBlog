using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpaceBlog.Pages.Error
{
    public class ServerErrorModel : PageModel
    {
        private readonly ILogger<ServerErrorModel> _logger;

        public ServerErrorModel(ILogger<ServerErrorModel> logger)
        {
            _logger = logger;
        }

        public string ErrorId { get; set; } = string.Empty;
        public string ErrorTime { get; set; } = string.Empty;
        public string? UserAgent { get; set; }
        public bool ShowTechnicalDetails { get; set; }

        public void OnGet()
        {
            // Генерируем уникальный ID ошибки для отслеживания
            ErrorId = Guid.NewGuid().ToString("N")[..8].ToUpper();
            ErrorTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            UserAgent = HttpContext.Request.Headers["User-Agent"].FirstOrDefault();
            
            // Показываем технические детали только для аутентифицированных пользователей
            ShowTechnicalDetails = User.Identity?.IsAuthenticated == true;

            _logger.LogError("Внутренняя ошибка сервера - ID: {ErrorId}, Пользователь: {User}, URL: {Url}", 
                ErrorId,
                User.Identity?.Name ?? "Anonymous",
                HttpContext.Request.Path + HttpContext.Request.QueryString);
        }
    }
}