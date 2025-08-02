using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SpaceBlog.Pages
{
    public class TestErrorsModel : PageModel
    {
        private readonly ILogger<TestErrorsModel> _logger;

        public TestErrorsModel(ILogger<TestErrorsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            _logger.LogInformation("Открыта страница тестирования ошибок");
        }

        public IActionResult OnPost(string errorType)
        {
            _logger.LogInformation("Имитация ошибки типа: {ErrorType}", errorType);

            return errorType switch
            {
                "403" => StatusCode(403, "Имитация ошибки 403 - Доступ запрещен"),
                "500" => StatusCode(500, "Имитация ошибки 500 - Внутренняя ошибка сервера"),
                "400" => StatusCode(400, "Имитация ошибки 400 - Неправильный запрос"),
                "401" => StatusCode(401, "Имитация ошибки 401 - Требуется авторизация"),
                _ => BadRequest("Неизвестный тип ошибки")
            };
        }
    }
}