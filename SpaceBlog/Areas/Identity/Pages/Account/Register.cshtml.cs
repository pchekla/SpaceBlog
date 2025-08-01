using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpaceBlog.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<BlogUser> _signInManager;
        private readonly UserManager<BlogUser> _userManager;
        private readonly IUserStore<BlogUser> _userStore;
        private readonly IUserEmailStore<BlogUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<BlogUser> userManager,
            IUserStore<BlogUser> userStore,
            SignInManager<BlogUser> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string? ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required(ErrorMessage = "Имя обязательно для заполнения")]
            [StringLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
            [Display(Name = "Имя")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Фамилия обязательна для заполнения")]
            [StringLength(100, ErrorMessage = "Фамилия не может быть длиннее 100 символов")]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Email обязателен для заполнения")]
            [EmailAddress(ErrorMessage = "Некорректный формат email адреса")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Пароль обязателен для заполнения")]
            [StringLength(100, ErrorMessage = "Пароль должен содержать минимум {2} и максимум {1} символов.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Подтвердите пароль")]
            [Compare("Password", ErrorMessage = "Пароли не совпадают")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                // Заполняем дополнительные поля BlogUser
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.DisplayName = $"{Input.FirstName} {Input.LastName}".Trim();
                user.RegistrationDate = DateTime.Now;
                user.EmailNotifications = true;
                user.IsPublicProfile = true;

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь создал новый аккаунт с паролем.");

                    // Назначаем базовую роль "User"
                    await _userManager.AddToRoleAsync(user, Role.Names.User);
                    _logger.LogInformation("Пользователю {Email} назначена роль User", Input.Email);

                    // Автоматический вход после регистрации
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("Пользователь {Email} автоматически вошел в систему после регистрации", Input.Email);

                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private BlogUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<BlogUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(BlogUser)}'. " +
                    $"Ensure that '{nameof(BlogUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<BlogUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<BlogUser>)_userStore;
        }
    }
}