using System.Net;
using System.Text.Json;

namespace SpaceBlog.Api.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Логируем исключение
            _logger.LogError(exception, "Необработанное исключение произошло. Путь: {Path}, Пользователь: {User}, IP: {IP}",
                context.Request.Path,
                context.User.Identity?.Name ?? "Anonymous",
                context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

            // Определяем статус код на основе типа исключения
            var statusCode = exception switch
            {
                ArgumentNullException => HttpStatusCode.BadRequest,
                ArgumentException => HttpStatusCode.BadRequest,
                UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                NotImplementedException => HttpStatusCode.NotImplemented,
                TimeoutException => HttpStatusCode.RequestTimeout,
                _ => HttpStatusCode.InternalServerError
            };

            // Устанавливаем статус код
            context.Response.StatusCode = (int)statusCode;

            // Если это API запрос, возвращаем JSON
            if (IsApiRequest(context))
            {
                await HandleApiExceptionAsync(context, exception, statusCode);
            }
            else
            {
                // Для обычных веб-запросов перенаправляем на страницу ошибки
                await HandleWebExceptionAsync(context, statusCode);
            }
        }

        private static bool IsApiRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/api") ||
                   context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                   context.Request.ContentType?.Contains("application/json") == true;
        }

        private async Task HandleApiExceptionAsync(HttpContext context, Exception exception, HttpStatusCode statusCode)
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = new
                {
                    message = GetUserFriendlyMessage(statusCode),
                    statusCode = (int)statusCode,
                    details = _environment.IsDevelopment() ? exception.Message : null,
                    stackTrace = _environment.IsDevelopment() ? exception.StackTrace : null,
                    timestamp = DateTime.UtcNow
                }
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private async Task HandleWebExceptionAsync(HttpContext context, HttpStatusCode statusCode)
        {
            // Сохраняем информацию об ошибке для отображения на странице ошибки
            context.Items["OriginalStatusCode"] = (int)statusCode;
            context.Items["OriginalPath"] = context.Request.Path.ToString();

            // Перенаправляем на соответствующую страницу ошибки
            var errorPath = $"/Error/{(int)statusCode}";
            context.Response.Redirect(errorPath);
            await Task.CompletedTask;
        }

        private static string GetUserFriendlyMessage(HttpStatusCode statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.BadRequest => "Неправильный запрос. Проверьте переданные данные.",
                HttpStatusCode.Unauthorized => "Требуется авторизация для доступа к этому ресурсу.",
                HttpStatusCode.Forbidden => "У вас нет прав доступа к этому ресурсу.",
                HttpStatusCode.NotFound => "Запрашиваемый ресурс не найден.",
                HttpStatusCode.RequestTimeout => "Время ожидания запроса истекло.",
                HttpStatusCode.InternalServerError => "Внутренняя ошибка сервера. Пожалуйста, попробуйте позже.",
                HttpStatusCode.NotImplemented => "Функциональность не реализована.",
                HttpStatusCode.BadGateway => "Ошибка шлюза. Пожалуйста, попробуйте позже.",
                HttpStatusCode.ServiceUnavailable => "Сервис временно недоступен.",
                _ => "Произошла ошибка при обработке запроса."
            };
        }
    }

    // Расширение для удобной регистрации middleware
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}