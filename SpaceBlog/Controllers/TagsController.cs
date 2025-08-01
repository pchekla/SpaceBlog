using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TagsController> _logger;

        public TagsController(ApplicationDbContext context, ILogger<TagsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Tags
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagResponseDto>>> GetTags(
            [FromQuery] bool includeInactive = false,
            [FromQuery] bool includeArticleCount = false)
        {
            try
            {
                var query = _context.Tags
                    .Include(t => t.CreatedBy)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(t => t.IsActive);
                }

                var tags = await query
                    .OrderBy(t => t.SortOrder)
                    .ThenBy(t => t.Name)
                    .Select(t => new TagResponseDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Description = t.Description,
                        Slug = t.Slug,
                        Color = t.Color,
                        Icon = t.Icon,
                        CreatedAt = t.CreatedAt,
                        IsActive = t.IsActive,
                        SortOrder = t.SortOrder,
                        CreatedBy = t.CreatedBy != null ? new CreatedByDto
                        {
                            Id = t.CreatedBy.Id,
                            DisplayName = t.CreatedBy.GetDisplayName()
                        } : null,
                        ArticlesCount = includeArticleCount ? 
                            t.ArticleTags.Count(at => at.Article!.IsPublished) : 0
                    })
                    .ToListAsync();

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка тегов");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Tags/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TagResponseDto>> GetTag(int id)
        {
            try
            {
                var tag = await _context.Tags
                    .Include(t => t.CreatedBy)
                    .Include(t => t.ArticleTags)
                        .ThenInclude(at => at.Article)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tag == null)
                {
                    return NotFound("Тег не найден");
                }

                var tagDto = new TagResponseDto
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Description = tag.Description,
                    Slug = tag.Slug,
                    Color = tag.Color,
                    Icon = tag.Icon,
                    CreatedAt = tag.CreatedAt,
                    IsActive = tag.IsActive,
                    SortOrder = tag.SortOrder,
                    CreatedBy = tag.CreatedBy != null ? new CreatedByDto
                    {
                        Id = tag.CreatedBy.Id,
                        DisplayName = tag.CreatedBy.GetDisplayName()
                    } : null,
                    ArticlesCount = tag.ArticleTags.Count(at => at.Article!.IsPublished)
                };

                return Ok(tagDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении тега {TagId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Tags/{id}/articles
        [HttpGet("{id}/articles")]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetTagArticles(
            int id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return NotFound("Тег не найден");
                }

                var query = _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Where(a => a.ArticleTags.Any(at => at.TagId == id) && a.IsPublished);

                var totalCount = await query.CountAsync();

                var articles = await query
                    .OrderByDescending(a => a.PublishedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new ArticleDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Summary = a.Summary,
                        Slug = a.Slug,
                        CreatedAt = a.CreatedAt,
                        PublishedAt = a.PublishedAt,
                        ViewCount = a.ViewCount,
                        ImageUrl = a.ImageUrl,
                        Author = new AuthorDto
                        {
                            Id = a.Author!.Id,
                            DisplayName = a.Author.GetDisplayName(),
                            AvatarUrl = a.Author.AvatarUrl
                        },
                        Tags = a.ArticleTags.Select(at => new TagDto
                        {
                            Id = at.Tag!.Id,
                            Name = at.Tag.Name,
                            Slug = at.Tag.Slug,
                            Color = at.Tag.Color
                        }).ToList(),
                        CommentsCount = a.Comments.Count(c => c.IsApproved)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Tag = new { Id = tag.Id, Name = tag.Name },
                    Articles = articles,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статей тега {TagId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Tags
        [HttpPost]
        public async Task<ActionResult<TagResponseDto>> CreateTag(CreateTagDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем уникальность имени
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == createDto.Name.ToLower());
                
                if (existingTag != null)
                {
                    return BadRequest("Тег с таким названием уже существует");
                }

                var tag = new Tag
                {
                    Name = createDto.Name,
                    Description = createDto.Description,
                    Slug = createDto.Slug ?? new Tag { Name = createDto.Name }.GenerateSlug(),
                    Color = createDto.Color ?? "#6c757d",
                    Icon = createDto.Icon,
                    SortOrder = createDto.SortOrder,
                    CreatedById = createDto.CreatedById,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Создан тег: {TagId} - {TagName}", tag.Id, tag.Name);

                var tagDto = new TagResponseDto
                {
                    Id = tag.Id,
                    Name = tag.Name,
                    Description = tag.Description,
                    Slug = tag.Slug,
                    Color = tag.Color,
                    Icon = tag.Icon,
                    CreatedAt = tag.CreatedAt,
                    IsActive = tag.IsActive,
                    SortOrder = tag.SortOrder,
                    ArticlesCount = 0
                };

                return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tagDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании тега");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // PUT: api/Tags/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, UpdateTagDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return NotFound("Тег не найден");
                }

                // Проверяем уникальность имени (кроме текущего тега)
                var existingTag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.Name.ToLower() == updateDto.Name.ToLower() && t.Id != id);
                
                if (existingTag != null)
                {
                    return BadRequest("Тег с таким названием уже существует");
                }

                tag.Name = updateDto.Name;
                tag.Description = updateDto.Description;
                tag.Slug = updateDto.Slug ?? tag.GenerateSlug();
                tag.Color = updateDto.Color ?? "#6c757d";
                tag.Icon = updateDto.Icon;
                tag.SortOrder = updateDto.SortOrder;
                tag.IsActive = updateDto.IsActive;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Обновлен тег: {TagId} - {TagName}", id, tag.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении тега {TagId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // DELETE: api/Tags/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var tag = await _context.Tags
                    .Include(t => t.ArticleTags)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (tag == null)
                {
                    return NotFound("Тег не найден");
                }

                // Проверяем, есть ли статьи с этим тегом
                if (tag.ArticleTags.Any())
                {
                    return BadRequest("Нельзя удалить тег, который используется в статьях. Сначала удалите связи с статьями.");
                }

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Удален тег: {TagId} - {TagName}", id, tag.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении тега {TagId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Tags/{id}/activate
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateTag(int id)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return NotFound("Тег не найден");
                }

                tag.Activate();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Активирован тег: {TagId} - {TagName}", id, tag.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при активации тега {TagId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Tags/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateTag(int id)
        {
            try
            {
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    return NotFound("Тег не найден");
                }

                tag.Deactivate();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Деактивирован тег: {TagId} - {TagName}", id, tag.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при деактивации тега {TagId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }

    // DTO классы для тегов
    public class TagResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Slug { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public CreatedByDto? CreatedBy { get; set; }
        public int ArticlesCount { get; set; }
    }

    public class CreateTagDto
    {
        [Required(ErrorMessage = "Название тега обязательно")]
        [StringLength(50, ErrorMessage = "Название тега не может быть длиннее 50 символов")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Описание не может быть длиннее 200 символов")]
        public string? Description { get; set; }

        [StringLength(60, ErrorMessage = "Slug не может быть длиннее 60 символов")]
        public string? Slug { get; set; }

        [StringLength(7, ErrorMessage = "Цвет должен быть в формате #RRGGBB")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Некорректный формат цвета")]
        public string? Color { get; set; }

        [StringLength(50, ErrorMessage = "Иконка не может быть длиннее 50 символов")]
        public string? Icon { get; set; }

        public int SortOrder { get; set; } = 0;
        public string? CreatedById { get; set; }
    }

    public class UpdateTagDto
    {
        [Required(ErrorMessage = "Название тега обязательно")]
        [StringLength(50, ErrorMessage = "Название тега не может быть длиннее 50 символов")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Описание не может быть длиннее 200 символов")]
        public string? Description { get; set; }

        [StringLength(60, ErrorMessage = "Slug не может быть длиннее 60 символов")]
        public string? Slug { get; set; }

        [StringLength(7, ErrorMessage = "Цвет должен быть в формате #RRGGBB")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Некорректный формат цвета")]
        public string? Color { get; set; }

        [StringLength(50, ErrorMessage = "Иконка не может быть длиннее 50 символов")]
        public string? Icon { get; set; }

        public int SortOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
    }

    public class CreatedByDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}