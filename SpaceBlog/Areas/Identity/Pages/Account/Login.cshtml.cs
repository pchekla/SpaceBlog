using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<BlogUser> _signInManager;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<BlogUser> signInManager, UserManager<BlogUser> userManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string? ReturnUrl { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Email обязателен для заполнения")]
            [EmailAddress(ErrorMessage = "Некорректный формат email")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Пароль обязателен для заполнения")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Запомнить меня")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
            
            // Убеждаемся, что Input инициализирован
            if (Input == null)
            {
                Input = new InputModel();
                _logger.LogInformation("Input модель была инициализирована в OnGet логина");
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation("=== НАЧАЛО POST ЛОГИНА ===");
            _logger.LogInformation("Input is null: {IsNull}", Input == null);
            _logger.LogInformation("Input.Email: '{Email}'", Input?.Email ?? "NULL");
            
            // Логируем данные формы
            _logger.LogInformation("=== ДАННЫЕ ФОРМЫ ЛОГИНА ===");
            foreach (var key in Request.Form.Keys)
            {
                _logger.LogInformation("Form[{Key}] = '{Value}'", key, Request.Form[key]);
            }
            
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            // Проверяем Input на null и инициализируем при необходимости
            if (Input == null)
            {
                _logger.LogError("Input модель была null, инициализируем новую");
                Input = new InputModel();
                
                // Попробуем заполнить из данных формы вручную
                if (Request.Form.ContainsKey("Input.Email"))
                {
                    Input.Email = Request.Form["Input.Email"].ToString();
                    Input.Password = Request.Form["Input.Password"].ToString();
                    Input.RememberMe = Request.Form["Input.RememberMe"].ToString().Contains("true");
                    
                    _logger.LogInformation("Данные восстановлены из формы: Email={Email}", Input.Email);
                }
                else
                {
                    _logger.LogError("Данные формы не найдены");
                    ModelState.AddModelError(string.Empty, "Ошибка обработки формы. Пожалуйста, попробуйте снова.");
                    return Page();
                }
            }

            if (ModelState.IsValid)
            {
                // Поиск пользователя по email
                var user = await _userManager.FindByEmailAsync(Input.Email ?? string.Empty);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                    return Page();
                }

                // Проверка на блокировку
                if (user.IsBanned)
                {
                    ModelState.AddModelError(string.Empty, $"Аккаунт заблокирован. Причина: {user.BanReason}");
                    return Page();
                }

                // Попытка входа
                var result = await _signInManager.PasswordSignInAsync(user, Input.Password ?? string.Empty, Input.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    // Обновляем дату последнего входа
                    user.UpdateLastLogin();
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("Пользователь {Email} вошел в систему.", Input.Email);
                    return LocalRedirect(returnUrl);
                }
                
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("Аккаунт пользователя {Email} заблокирован.", Input.Email);
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неверный email или пароль.");
                    return Page();
                }
            }

            return Page();
        }
    }
}
