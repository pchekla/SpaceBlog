using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SpaceBlog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<BlogUser> _userManager;
        private readonly SignInManager<BlogUser> _signInManager;
        private readonly RoleManager<Role> _roleManager;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<BlogUser> userManager,
            SignInManager<BlogUser> signInManager,
            RoleManager<Role> roleManager,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // POST: api/Auth/login
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login(LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    return BadRequest("Неверный email или пароль");
                }

                if (user.IsBanned)
                {
                    return BadRequest($"Аккаунт заблокирован. Причина: {user.BanReason}");
                }

                var result = await _signInManager.PasswordSignInAsync(user, loginDto.Password, loginDto.RememberMe, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    // Обновляем дату последнего входа
                    user.UpdateLastLogin();
                    await _userManager.UpdateAsync(user);

                    // Получаем роли пользователя
                    var roles = await _userManager.GetRolesAsync(user);
                    var primaryRole = await user.GetPrimaryRole(_userManager);

                    // Добавляем роли в claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName ?? ""),
                        new Claim(ClaimTypes.Email, user.Email ?? ""),
                        new Claim("DisplayName", user.GetDisplayName()),
                        new Claim("PrimaryRole", primaryRole)
                    };

                    // Добавляем все роли как отдельные claims
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role));
                    }

                    // Добавляем дополнительные claims к пользователю
                    await _userManager.AddClaimsAsync(user, claims.Where(c => 
                        !c.Type.Equals(ClaimTypes.NameIdentifier) && 
                        !c.Type.Equals(ClaimTypes.Name) && 
                        !c.Type.Equals(ClaimTypes.Email)));

                    _logger.LogInformation("Пользователь {Email} успешно авторизован с ролями: {Roles}", 
                        loginDto.Email, string.Join(", ", roles));

                    var response = new LoginResponseDto
                    {
                        Success = true,
                        Message = "Вход выполнен успешно",
                        User = new AuthUserDto
                        {
                            Id = user.Id,
                            Email = user.Email!,
                            DisplayName = user.GetDisplayName(),
                            Roles = roles.ToList(),
                            PrimaryRole = primaryRole,
                            AvatarUrl = user.AvatarUrl,
                            LastLoginDate = user.LastLoginDate
                        }
                    };

                    return Ok(response);
                }

                if (result.IsLockedOut)
                {
                    return BadRequest("Аккаунт временно заблокирован из-за множественных неудачных попыток входа");
                }

                if (result.IsNotAllowed)
                {
                    return BadRequest("Вход не разрешен. Возможно, требуется подтверждение email");
                }

                return BadRequest("Неверный email или пароль");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при авторизации пользователя {Email}", loginDto.Email);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("Пользователь вышел из системы");
                
                return Ok(new { Success = true, Message = "Выход выполнен успешно" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Auth/me
        [HttpGet("me")]
        public async Task<ActionResult<AuthUserDto>> GetCurrentUser()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Unauthorized("Пользователь не авторизован");
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("Не удалось определить пользователя");
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                var roles = await _userManager.GetRolesAsync(user);
                var primaryRole = await user.GetPrimaryRole(_userManager);

                var userDto = new AuthUserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    DisplayName = user.GetDisplayName(),
                    Roles = roles.ToList(),
                    PrimaryRole = primaryRole,
                    AvatarUrl = user.AvatarUrl,
                    LastLoginDate = user.LastLoginDate
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении текущего пользователя");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Auth/roles
        [HttpGet("roles")]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles()
        {
            try
            {
                var roles = await _roleManager.Roles
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Priority)
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name!,
                        Description = r.Description,
                        Priority = r.Priority
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка ролей");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Auth/assign-role
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole(AssignRoleDto assignRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(assignRoleDto.UserId);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                var role = await _roleManager.FindByNameAsync(assignRoleDto.RoleName);
                if (role == null)
                {
                    return NotFound("Роль не найдена");
                }

                if (await _userManager.IsInRoleAsync(user, assignRoleDto.RoleName))
                {
                    return BadRequest("Пользователь уже имеет эту роль");
                }

                var result = await _userManager.AddToRoleAsync(user, assignRoleDto.RoleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Пользователю {UserId} назначена роль {RoleName}", 
                        assignRoleDto.UserId, assignRoleDto.RoleName);
                    
                    return Ok(new { Success = true, Message = "Роль назначена успешно" });
                }

                return BadRequest("Не удалось назначить роль");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при назначении роли");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Auth/remove-role
        [HttpPost("remove-role")]
        public async Task<IActionResult> RemoveRole(AssignRoleDto removeRoleDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(removeRoleDto.UserId);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                if (!await _userManager.IsInRoleAsync(user, removeRoleDto.RoleName))
                {
                    return BadRequest("Пользователь не имеет этой роли");
                }

                var result = await _userManager.RemoveFromRoleAsync(user, removeRoleDto.RoleName);
                if (result.Succeeded)
                {
                    _logger.LogInformation("У пользователя {UserId} отозвана роль {RoleName}", 
                        removeRoleDto.UserId, removeRoleDto.RoleName);
                    
                    return Ok(new { Success = true, Message = "Роль отозвана успешно" });
                }

                return BadRequest("Не удалось отозвать роль");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отзыве роли");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }

    // DTO классы для аутентификации
    public class LoginDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; } = false;
    }

    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AuthUserDto? User { get; set; }
    }

    public class AuthUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string PrimaryRole { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class RoleDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Priority { get; set; }
    }

    public class AssignRoleDto
    {
        [Required(ErrorMessage = "ID пользователя обязателен")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Название роли обязательно")]
        public string RoleName { get; set; } = string.Empty;
    }
}