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
            
            // Убеждаемся, что Input инициализирован
            if (Input == null)
            {
                Input = new InputModel();
                _logger.LogInformation("Input модель была инициализирована в OnGet");
            }
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            try
            {
                _logger.LogInformation("=== НАЧАЛО POST ЗАПРОСА ===");
                _logger.LogInformation("Input is null: {IsNull}", Input == null);
                _logger.LogInformation("Input.Email: '{Email}'", Input?.Email ?? "NULL");
                _logger.LogInformation("Input.FirstName: '{FirstName}'", Input?.FirstName ?? "NULL");
                _logger.LogInformation("Input.LastName: '{LastName}'", Input?.LastName ?? "NULL");
                
                // Логируем данные формы
                _logger.LogInformation("=== ДАННЫЕ ФОРМЫ ===");
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
                    
                    if (Request.Form.ContainsKey("Input.Email"))
                    {
                        Input.Email = Request.Form["Input.Email"];
                        Input.FirstName = Request.Form["Input.FirstName"];
                        Input.LastName = Request.Form["Input.LastName"];
                        Input.Password = Request.Form["Input.Password"];
                        Input.ConfirmPassword = Request.Form["Input.ConfirmPassword"];
                        
                        _logger.LogInformation("Данные восстановлены из формы: Email={Email}, FirstName={FirstName}", 
                            Input.Email, Input.FirstName);
                    }
                    else
                    {
                        _logger.LogError("Данные формы не найдены");
                        ModelState.AddModelError(string.Empty, "Ошибка обработки формы. Пожалуйста, попробуйте снова.");
                        return Page();
                    }
                }

                // Проверяем валидность модели
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Модель не прошла валидацию. Ошибки: {Errors}", 
                        string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                    return Page();
                }

                _logger.LogInformation("Модель прошла валидацию для email: {Email}", Input.Email);

                // Проверяем, не существует ли уже пользователь с таким email
                var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Попытка регистрации с уже существующим email: {Email}", Input.Email);
                    ModelState.AddModelError(string.Empty, "Пользователь с таким email уже существует");
                    return Page();
                }

                _logger.LogInformation("Email свободен, создаём пользователя: {Email}", Input.Email);
                var user = CreateUser();

                // Заполняем дополнительные поля BlogUser
                user.FirstName = Input.FirstName;
                user.LastName = Input.LastName;
                user.DisplayName = $"{Input.FirstName} {Input.LastName}".Trim();
                user.RegistrationDate = DateTime.Now;
                user.EmailNotifications = true;
                user.IsPublicProfile = true;

                _logger.LogInformation("Настройка пользователя: {Email}", Input.Email);
                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);
                
                _logger.LogInformation("Создание пользователя в базе данных: {Email}", Input.Email);
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователь {Email} успешно создан", Input.Email);

                    try
                    {
                        // Назначаем базовую роль "User"
                        await _userManager.AddToRoleAsync(user, Role.Names.User);
                        _logger.LogInformation("Пользователю {Email} назначена роль User", Input.Email);

                        // Автоматический вход после регистрации
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("Пользователь {Email} автоматически вошел в систему после регистрации", Input.Email);

                        return LocalRedirect(returnUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при назначении роли или входе для пользователя {Email}", Input.Email);
                        ModelState.AddModelError(string.Empty, "Ошибка при завершении регистрации. Обратитесь к администратору.");
                        return Page();
                    }
                }
                else
                {
                    _logger.LogWarning("Ошибка создания пользователя {Email}. Ошибки: {Errors}", 
                        Input.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Критическая ошибка при регистрации пользователя {Email}", Input?.Email ?? "неизвестно");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при регистрации. Пожалуйста, попробуйте позже.");
                return Page();
            }
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