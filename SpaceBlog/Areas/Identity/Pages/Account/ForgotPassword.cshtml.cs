using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(UserManager<BlogUser> userManager, ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Email обязателен для заполнения")]
            [EmailAddress(ErrorMessage = "Некорректный формат email")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    ModelState.AddModelError(string.Empty, 
                        "В демонстрационной версии восстановление пароля недоступно. " +
                        "Используйте тестовые аккаунты для входа в систему.");
                    return Page();
                }

                // В реальном приложении здесь бы отправлялось письмо с восстановлением
                ModelState.AddModelError(string.Empty, 
                    "В демонстрационной версии восстановление пароля недоступно. " +
                    "Используйте тестовые аккаунты для входа в систему.");
                
                _logger.LogInformation("Запрос восстановления пароля для {Email}", Input.Email);
            }

            return Page();
        }
    }
}
