using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArticlesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ArticlesController> _logger;

        public ArticlesController(ApplicationDbContext context, ILogger<ArticlesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Articles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticles(
            [FromQuery] bool includeUnpublished = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .AsQueryable();

                if (!includeUnpublished)
                {
                    query = query.Where(a => a.IsPublished);
                }

                var totalCount = await query.CountAsync();
                
                var articles = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new ArticleDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Content = a.Content,
                        Summary = a.Summary,
                        Slug = a.Slug,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt,
                        IsPublished = a.IsPublished,
                        PublishedAt = a.PublishedAt,
                        ViewCount = a.ViewCount,
                        ImageUrl = a.ImageUrl,
                        MetaDescription = a.MetaDescription,
                        Keywords = a.Keywords,
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
                            Color = at.Tag.Color,
                            Icon = at.Tag.Icon
                        }).ToList(),
                        CommentsCount = a.Comments.Count(c => c.IsApproved)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Articles = articles,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка статей");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Articles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ArticleDto>> GetArticle(int id)
        {
            try
            {
                var article = await _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Include(a => a.Comments.Where(c => c.IsApproved))
                        .ThenInclude(c => c.Author)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null)
                {
                    return NotFound("Статья не найдена");
                }

                // Увеличиваем счетчик просмотров
                article.IncrementViewCount();
                await _context.SaveChangesAsync();

                var articleDto = new ArticleDto
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    Summary = article.Summary,
                    Slug = article.Slug,
                    CreatedAt = article.CreatedAt,
                    UpdatedAt = article.UpdatedAt,
                    IsPublished = article.IsPublished,
                    PublishedAt = article.PublishedAt,
                    ViewCount = article.ViewCount,
                    ImageUrl = article.ImageUrl,
                    MetaDescription = article.MetaDescription,
                    Keywords = article.Keywords,
                    Author = new AuthorDto
                    {
                        Id = article.Author!.Id,
                        DisplayName = article.Author.GetDisplayName(),
                        AvatarUrl = article.Author.AvatarUrl
                    },
                    Tags = article.ArticleTags.Select(at => new TagDto
                    {
                        Id = at.Tag!.Id,
                        Name = at.Tag.Name,
                        Slug = at.Tag.Slug,
                        Color = at.Tag.Color,
                        Icon = at.Tag.Icon
                    }).ToList(),
                    CommentsCount = article.Comments.Count(c => c.IsApproved)
                };

                return Ok(articleDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статьи {ArticleId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Articles/author/{authorId}
        [HttpGet("author/{authorId}")]
        public async Task<ActionResult<IEnumerable<ArticleDto>>> GetArticlesByAuthor(
            string authorId,
            [FromQuery] bool includeUnpublished = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Articles
                    .Include(a => a.Author)
                    .Include(a => a.ArticleTags)
                        .ThenInclude(at => at.Tag)
                    .Where(a => a.AuthorId == authorId);

                if (!includeUnpublished)
                {
                    query = query.Where(a => a.IsPublished);
                }

                var totalCount = await query.CountAsync();

                var articles = await query
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new ArticleDto
                    {
                        Id = a.Id,
                        Title = a.Title,
                        Summary = a.Summary,
                        Slug = a.Slug,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt,
                        IsPublished = a.IsPublished,
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
                    Articles = articles,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении статей автора {AuthorId}", authorId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Articles
        [HttpPost]
        public async Task<ActionResult<ArticleDto>> CreateArticle(CreateArticleDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем, существует ли автор
                var author = await _context.Users.FindAsync(createDto.AuthorId);
                if (author == null)
                {
                    return BadRequest("Автор не найден");
                }

                var article = new Article
                {
                    Title = createDto.Title,
                    Content = createDto.Content,
                    Summary = createDto.Summary,
                    Slug = createDto.Slug ?? new Article { Title = createDto.Title }.GenerateSlug(),
                    AuthorId = createDto.AuthorId,
                    ImageUrl = createDto.ImageUrl,
                    MetaDescription = createDto.MetaDescription,
                    Keywords = createDto.Keywords,
                    IsPublished = createDto.IsPublished,
                    CreatedAt = DateTime.Now
                };

                if (createDto.IsPublished)
                {
                    article.Publish();
                }

                _context.Articles.Add(article);
                await _context.SaveChangesAsync();

                // Добавляем теги, если есть
                if (createDto.TagIds != null && createDto.TagIds.Any())
                {
                    foreach (var tagId in createDto.TagIds)
                    {
                        var articleTag = new ArticleTag
                        {
                            ArticleId = article.Id,
                            TagId = tagId,
                            CreatedById = createDto.AuthorId
                        };
                        _context.ArticleTags.Add(articleTag);
                    }
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Создана статья: {ArticleId} пользователем {AuthorId}", article.Id, createDto.AuthorId);

                return CreatedAtAction(nameof(GetArticle), new { id = article.Id }, new ArticleDto
                {
                    Id = article.Id,
                    Title = article.Title,
                    Content = article.Content,
                    Summary = article.Summary,
                    Slug = article.Slug,
                    CreatedAt = article.CreatedAt,
                    IsPublished = article.IsPublished,
                    PublishedAt = article.PublishedAt,
                    ViewCount = article.ViewCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании статьи");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // PUT: api/Articles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(int id, UpdateArticleDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var article = await _context.Articles
                    .Include(a => a.ArticleTags)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (article == null)
                {
                    return NotFound("Статья не найдена");
                }

                article.Title = updateDto.Title;
                article.Content = updateDto.Content;
                article.Summary = updateDto.Summary;
                article.Slug = updateDto.Slug ?? article.GenerateSlug();
                article.ImageUrl = updateDto.ImageUrl;
                article.MetaDescription = updateDto.MetaDescription;
                article.Keywords = updateDto.Keywords;
                article.UpdatedAt = DateTime.Now;

                // Обновляем статус публикации
                if (updateDto.IsPublished && !article.IsPublished)
                {
                    article.Publish();
                }
                else if (!updateDto.IsPublished && article.IsPublished)
                {
                    article.Unpublish();
                }

                // Обновляем теги
                if (updateDto.TagIds != null)
                {
                    // Удаляем старые связи
                    _context.ArticleTags.RemoveRange(article.ArticleTags);

                    // Добавляем новые связи
                    foreach (var tagId in updateDto.TagIds)
                    {
                        var articleTag = new ArticleTag
                        {
                            ArticleId = article.Id,
                            TagId = tagId,
                            CreatedById = article.AuthorId
                        };
                        _context.ArticleTags.Add(articleTag);
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Обновлена статья: {ArticleId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении статьи {ArticleId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // DELETE: api/Articles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteArticle(int id)
        {
            try
            {
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                {
                    return NotFound("Статья не найдена");
                }

                _context.Articles.Remove(article);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Удалена статья: {ArticleId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении статьи {ArticleId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Articles/{id}/publish
        [HttpPost("{id}/publish")]
        public async Task<IActionResult> PublishArticle(int id)
        {
            try
            {
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                {
                    return NotFound("Статья не найдена");
                }

                article.Publish();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Опубликована статья: {ArticleId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при публикации статьи {ArticleId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Articles/{id}/unpublish
        [HttpPost("{id}/unpublish")]
        public async Task<IActionResult> UnpublishArticle(int id)
        {
            try
            {
                var article = await _context.Articles.FindAsync(id);
                if (article == null)
                {
                    return NotFound("Статья не найдена");
                }

                article.Unpublish();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Снята с публикации статья: {ArticleId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при снятии с публикации статьи {ArticleId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }

    // DTO классы для статей
    public class ArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Slug { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
        public string? ImageUrl { get; set; }
        public string? MetaDescription { get; set; }
        public string? Keywords { get; set; }
        public AuthorDto? Author { get; set; }
        public List<TagDto> Tags { get; set; } = new();
        public int CommentsCount { get; set; }
    }

    public class CreateArticleDto
    {
        [Required(ErrorMessage = "Заголовок обязателен")]
        [StringLength(200, ErrorMessage = "Заголовок не может быть длиннее 200 символов")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Содержание обязательно")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Краткое описание не может быть длиннее 500 символов")]
        public string? Summary { get; set; }

        [StringLength(250, ErrorMessage = "Slug не может быть длиннее 250 символов")]
        public string? Slug { get; set; }

        [Required(ErrorMessage = "Автор обязателен")]
        public string AuthorId { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "URL изображения не может быть длиннее 500 символов")]
        [Url(ErrorMessage = "Некорректный URL изображения")]
        public string? ImageUrl { get; set; }

        [StringLength(160, ErrorMessage = "Мета-описание не может быть длиннее 160 символов")]
        public string? MetaDescription { get; set; }

        [StringLength(200, ErrorMessage = "Ключевые слова не могут быть длиннее 200 символов")]
        public string? Keywords { get; set; }

        public bool IsPublished { get; set; } = false;
        public List<int>? TagIds { get; set; }
    }

    public class UpdateArticleDto
    {
        [Required(ErrorMessage = "Заголовок обязателен")]
        [StringLength(200, ErrorMessage = "Заголовок не может быть длиннее 200 символов")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Содержание обязательно")]
        public string Content { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Краткое описание не может быть длиннее 500 символов")]
        public string? Summary { get; set; }

        [StringLength(250, ErrorMessage = "Slug не может быть длиннее 250 символов")]
        public string? Slug { get; set; }

        [StringLength(500, ErrorMessage = "URL изображения не может быть длиннее 500 символов")]
        [Url(ErrorMessage = "Некорректный URL изображения")]
        public string? ImageUrl { get; set; }

        [StringLength(160, ErrorMessage = "Мета-описание не может быть длиннее 160 символов")]
        public string? MetaDescription { get; set; }

        [StringLength(200, ErrorMessage = "Ключевые слова не могут быть длиннее 200 символов")]
        public string? Keywords { get; set; }

        public bool IsPublished { get; set; } = false;
        public List<int>? TagIds { get; set; }
    }

    public class AuthorDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
    }
}