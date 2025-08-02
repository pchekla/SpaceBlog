using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpaceBlog.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class ErrorModel : PageModel
    {
        public string? RequestId { get; set; }
        public new int? StatusCode { get; set; }
        public string? OriginalPath { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet(int? statusCode = null)
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            StatusCode = statusCode ?? HttpContext.Response.StatusCode;
            OriginalPath = HttpContext.Request.Path + HttpContext.Request.QueryString;
            
            _logger.LogWarning("Ошибка {StatusCode} для пути: {Path} - пользователь: {User}", 
                StatusCode, 
                OriginalPath,
                User.Identity?.Name ?? "Anonymous");
        }
    }

}
