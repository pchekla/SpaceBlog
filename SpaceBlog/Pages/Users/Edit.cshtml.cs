using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Pages.Users
{
    [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут редактировать пользователей
    public class EditModel : PageModel
    {
        private readonly UserManager<BlogUser> _userManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(UserManager<BlogUser> userManager, RoleManager<Role> roleManager, ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public List<string> SelectedRoleIds { get; set; } = new();

        public List<RoleViewModel> AvailableRoles { get; set; } = new();
        public BlogUser EditingUser { get; set; } = null!;

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

            [Required(ErrorMessage = "Email обязателен для заполнения")]
            [EmailAddress(ErrorMessage = "Некорректный формат email")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать минимум {2} символов")]
            [DataType(DataType.Password)]
            [Display(Name = "Пароль")]
            public string? Password { get; set; }
        }

        public class RoleViewModel
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var editingUser = await _userManager.FindByIdAsync(id);
            if (editingUser == null)
            {
                return NotFound();
            }
            EditingUser = editingUser;

            await LoadDataAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var editingUser = await _userManager.FindByIdAsync(id);
            if (editingUser == null)
            {
                return NotFound();
            }
            EditingUser = editingUser;

            await LoadDataAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Обновляем основную информацию пользователя
                EditingUser.FirstName = Input.FirstName.Trim();
                EditingUser.LastName = Input.LastName.Trim();
                EditingUser.Email = Input.Email.Trim();
                EditingUser.UserName = Input.Email.Trim();
                EditingUser.DisplayName = $"{EditingUser.FirstName} {EditingUser.LastName}".Trim();

                var updateResult = await _userManager.UpdateAsync(EditingUser);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return Page();
                }

                // Обновляем пароль, если указан
                if (!string.IsNullOrWhiteSpace(Input.Password))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(EditingUser);
                    var passwordResult = await _userManager.ResetPasswordAsync(EditingUser, token, Input.Password);
                    if (!passwordResult.Succeeded)
                    {
                        foreach (var error in passwordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }

                // Обновляем роли пользователя
                var currentRoles = await _userManager.GetRolesAsync(EditingUser);
                var selectedRoleNames = new List<string>();
                
                foreach (var roleId in SelectedRoleIds)
                {
                    var role = await _roleManager.FindByIdAsync(roleId);
                    if (role != null)
                    {
                        selectedRoleNames.Add(role.Name!);
                    }
                }

                // Удаляем старые роли
                var rolesToRemove = currentRoles.Except(selectedRoleNames).ToList();
                if (rolesToRemove.Any())
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(EditingUser, rolesToRemove);
                    if (!removeResult.Succeeded)
                    {
                        foreach (var error in removeResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }

                // Добавляем новые роли
                var rolesToAdd = selectedRoleNames.Except(currentRoles).ToList();
                if (rolesToAdd.Any())
                {
                    var addResult = await _userManager.AddToRolesAsync(EditingUser, rolesToAdd);
                    if (!addResult.Succeeded)
                    {
                        foreach (var error in addResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return Page();
                    }
                }

                _logger.LogInformation($"Пользователь {EditingUser.DisplayName} (ID: {EditingUser.Id}) обновлен");

                StatusMessage = $"Пользователь \"{EditingUser.DisplayName}\" успешно обновлен!";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя");
                ModelState.AddModelError(string.Empty, "Произошла ошибка при обновлении пользователя. Попробуйте еще раз.");
                return Page();
            }
        }

        private async Task LoadDataAsync()
        {
            // Загружаем информацию о пользователе в форму
            Input.FirstName = EditingUser.FirstName ?? "";
            Input.LastName = EditingUser.LastName ?? "";
            Input.Email = EditingUser.Email ?? "";

            // Загружаем доступные роли
            AvailableRoles = await _roleManager.Roles
                .Select(r => new RoleViewModel
                {
                    Id = r.Id,
                    Name = r.Name ?? "",
                    DisplayName = GetRoleDisplayName(r.Name ?? "")
                })
                .OrderBy(r => r.DisplayName)
                .ToListAsync();

            // Загружаем текущие роли пользователя
            var currentRoles = await _userManager.GetRolesAsync(EditingUser);
            SelectedRoleIds = new List<string>();
            
            foreach (var roleName in currentRoles)
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role != null)
                {
                    SelectedRoleIds.Add(role.Id);
                }
            }
        }

        private static string GetRoleDisplayName(string roleName)
        {
            return roleName switch
            {
                Role.Names.Administrator => Role.DisplayNames.Administrator,
                Role.Names.Moderator => Role.DisplayNames.Moderator,
                Role.Names.Author => Role.DisplayNames.Author,
                Role.Names.User => Role.DisplayNames.User,
                _ => roleName
            };
        }
    }
}