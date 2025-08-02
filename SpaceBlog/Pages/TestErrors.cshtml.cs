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

        public IActionResult OnPost(string? errorType, string? exceptionType)
        {
            if (!string.IsNullOrEmpty(errorType))
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

            if (!string.IsNullOrEmpty(exceptionType))
            {
                _logger.LogInformation("Имитация исключения типа: {ExceptionType}", exceptionType);

                // Генерируем исключения для тестирования глобального обработчика
                switch (exceptionType)
                {
                    case "ArgumentNull":
                        throw new ArgumentNullException("testParameter", "Тестовое ArgumentNullException из TestErrors страницы");
                    
                    case "Unauthorized":
                        throw new UnauthorizedAccessException("Тестовое UnauthorizedAccessException из TestErrors страницы");
                    
                    case "NotImplemented":
                        throw new NotImplementedException("Тестовое NotImplementedException из TestErrors страницы");
                    
                    case "General":
                        throw new InvalidOperationException("Тестовое общее исключение из TestErrors страницы");
                    
                    default:
                        throw new Exception($"Неизвестный тип исключения: {exceptionType}");
                }
            }

            return BadRequest("Не указан тип ошибки или исключения");
        }
    }
}