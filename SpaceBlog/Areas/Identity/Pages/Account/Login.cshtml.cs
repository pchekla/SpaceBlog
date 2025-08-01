using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpaceBlog.Models;
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
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                // Поиск пользователя по email
                var user = await _userManager.FindByEmailAsync(Input.Email);
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
                var result = await _signInManager.PasswordSignInAsync(user, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                
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