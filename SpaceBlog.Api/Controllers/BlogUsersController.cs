using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        private readonly ILogger<BlogUsersController> _logger;

        public BlogUsersController(
            ApplicationDbContext context, 
            UserManager<BlogUser> userManager,
            ILogger<BlogUsersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/BlogUsers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BlogUserDto>>> GetUsers()
        {
            try
            {
                var users = await _context.Users
                    .Where(u => !u.IsBanned)
                    .Select(u => new BlogUserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        DisplayName = u.DisplayName,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email!,
                        Bio = u.Bio,
                        AvatarUrl = u.AvatarUrl,
                        Website = u.Website,
                        Location = u.Location,
                        RegistrationDate = u.RegistrationDate,
                        LastLoginDate = u.LastLoginDate,
                        Role = "Пользователь",
                        PublishedArticlesCount = u.PublishedArticlesCount,
                        IsPublicProfile = u.IsPublicProfile
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка пользователей");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/BlogUsers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<BlogUserDto>> GetUser(string id)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == id && !u.IsBanned)
                    .Select(u => new BlogUserDto
                    {
                        Id = u.Id,
                        UserName = u.UserName!,
                        DisplayName = u.DisplayName,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Email = u.Email!,
                        Bio = u.Bio,
                        AvatarUrl = u.AvatarUrl,
                        Website = u.Website,
                        Location = u.Location,
                        BirthDate = u.BirthDate,
                        RegistrationDate = u.RegistrationDate,
                        LastLoginDate = u.LastLoginDate,
                        Role = "Пользователь",
                        PublishedArticlesCount = u.PublishedArticlesCount,
                        TotalCommentsCount = u.TotalCommentsCount,
                        IsPublicProfile = u.IsPublicProfile
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении пользователя {UserId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/BlogUsers/register
        [HttpPost("register")]
        public async Task<ActionResult<BlogUserDto>> Register(RegisterUserDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем, существует ли пользователь с таким email
                var existingUser = await _userManager.FindByEmailAsync(registerDto.Email);
                if (existingUser != null)
                {
                    return BadRequest("Пользователь с таким email уже существует");
                }

                // Создаем нового пользователя
                var user = new BlogUser
                {
                    UserName = registerDto.Email,
                    Email = registerDto.Email,
                    FirstName = registerDto.FirstName,
                    LastName = registerDto.LastName,
                    DisplayName = registerDto.DisplayName ?? $"{registerDto.FirstName} {registerDto.LastName}".Trim(),
                    // Роль будет назначена отдельно через RoleManager
                    RegistrationDate = DateTime.Now,
                    EmailNotifications = true,
                    IsPublicProfile = true
                };

                var result = await _userManager.CreateAsync(user, registerDto.Password);

                if (result.Succeeded)
                {
                    // Назначаем базовую роль "Пользователь"
                    await _userManager.AddToRoleAsync(user, Role.Names.User);
                    
                    _logger.LogInformation("Создан новый пользователь: {Email}", registerDto.Email);

                    var userDto = new BlogUserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.DisplayName,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Email = user.Email,
                        RegistrationDate = user.RegistrationDate,
                        Role = Role.DisplayNames.User,
                        IsPublicProfile = user.IsPublicProfile
                    };

                    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, userDto);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // PUT: api/BlogUsers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                // Обновляем поля
                user.FirstName = updateDto.FirstName;
                user.LastName = updateDto.LastName;
                user.DisplayName = updateDto.DisplayName ?? $"{updateDto.FirstName} {updateDto.LastName}".Trim();
                user.Bio = updateDto.Bio;
                user.AvatarUrl = updateDto.AvatarUrl;
                user.Website = updateDto.Website;
                user.Location = updateDto.Location;
                user.BirthDate = updateDto.BirthDate;
                user.IsPublicProfile = updateDto.IsPublicProfile;
                user.EmailNotifications = updateDto.EmailNotifications;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Обновлен пользователь: {UserId}", id);
                    return NoContent();
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пользователя {UserId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // DELETE: api/BlogUsers/{id}
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdministrator")] // Только администраторы могут удалять пользователей
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                var result = await _userManager.DeleteAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Удален пользователь: {UserId}", id);
                    return NoContent();
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return BadRequest(ModelState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении пользователя {UserId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/BlogUsers/{id}/ban
        [HttpPost("{id}/ban")]
        public async Task<IActionResult> BanUser(string id, BanUserDto banDto)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                user.Ban(banDto.Reason);
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Заблокирован пользователь: {UserId}, причина: {Reason}", id, banDto.Reason);
                    return NoContent();
                }

                return BadRequest("Не удалось заблокировать пользователя");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при блокировке пользователя {UserId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/BlogUsers/{id}/unban
        [HttpPost("{id}/unban")]
        public async Task<IActionResult> UnbanUser(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound("Пользователь не найден");
                }

                user.Unban();
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Разблокирован пользователь: {UserId}", id);
                    return NoContent();
                }

                return BadRequest("Не удалось разблокировать пользователя");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при разблокировке пользователя {UserId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }

    // DTO классы для пользователей
    public class BlogUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string Role { get; set; } = string.Empty;
        public int PublishedArticlesCount { get; set; }
        public int TotalCommentsCount { get; set; }
        public bool IsPublicProfile { get; set; }
    }

    public class RegisterUserDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный формат email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать от 6 до 100 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, ErrorMessage = "Имя не может быть длиннее 50 символов")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, ErrorMessage = "Фамилия не может быть длиннее 50 символов")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Отображаемое имя не может быть длиннее 100 символов")]
        public string? DisplayName { get; set; }
    }

    public class UpdateUserDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(50, ErrorMessage = "Имя не может быть длиннее 50 символов")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Фамилия обязательна")]
        [StringLength(50, ErrorMessage = "Фамилия не может быть длиннее 50 символов")]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Отображаемое имя не может быть длиннее 100 символов")]
        public string? DisplayName { get; set; }

        [StringLength(1000, ErrorMessage = "Описание не может быть длиннее 1000 символов")]
        public string? Bio { get; set; }

        [StringLength(500, ErrorMessage = "URL аватара не может быть длиннее 500 символов")]
        [Url(ErrorMessage = "Некорректный URL аватара")]
        public string? AvatarUrl { get; set; }

        [StringLength(200, ErrorMessage = "URL веб-сайта не может быть длиннее 200 символов")]
        [Url(ErrorMessage = "Некорректный URL веб-сайта")]
        public string? Website { get; set; }

        [StringLength(100, ErrorMessage = "Местоположение не может быть длиннее 100 символов")]
        public string? Location { get; set; }

        public DateTime? BirthDate { get; set; }
        public bool IsPublicProfile { get; set; } = true;
        public bool EmailNotifications { get; set; } = true;
    }

    public class BanUserDto
    {
        [Required(ErrorMessage = "Причина блокировки обязательна")]
        [StringLength(500, ErrorMessage = "Причина блокировки не может быть длиннее 500 символов")]
        public string Reason { get; set; } = string.Empty;
    }
}