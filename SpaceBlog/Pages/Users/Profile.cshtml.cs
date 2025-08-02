using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Users
{
    [Authorize] // Только авторизованные пользователи могут редактировать свой профиль
    public class ProfileModel : PageModel
    {
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(UserManager<BlogUser> userManager, ILogger<ProfileModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public BlogUser CurrentUser { get; set; } = null!;

        [TempData]
        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Имя обязательно для заполнения")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Имя должно содержать от {2} до {1} символов")]
            [Display(Name = "Имя")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "Фамилия обязательна для заполнения")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Фамилия должна содержать от {2} до {1} символов")]
            [Display(Name = "Фамилия")]
            public string LastName { get; set; } = string.Empty;

            [StringLength(200, ErrorMessage = "Биография не может быть длиннее {1} символов")]
            [Display(Name = "О себе")]
            public string? Bio { get; set; }

            [Display(Name = "Дата рождения")]
            [DataType(DataType.Date)]
            public DateTime? BirthDate { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            CurrentUser = user;

            // Заполняем форму текущими данными пользователя
            Input.FirstName = user.FirstName ?? "";
            Input.LastName = user.LastName ?? "";
            Input.Bio = user.Bio ?? "";
            Input.BirthDate = user.BirthDate;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            CurrentUser = user;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Обновляем данные пользователя
                user.FirstName = Input.FirstName.Trim();
                user.LastName = Input.LastName.Trim();
                user.Bio = Input.Bio?.Trim();
                user.BirthDate = Input.BirthDate;
                
                // Обновляем отображаемое имя
                user.DisplayName = $"{user.FirstName} {user.LastName}".Trim();

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    _logger.LogInformation($"Пользователь {user.DisplayName} (ID: {user.Id}) обновил свой профиль");
                    StatusMessage = "Ваш профиль успешно обновлен!";
                    return RedirectToPage();
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении профиля пользователя {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении профиля. Попробуйте еще раз.");
            }

            return Page();
        }
    }
}
