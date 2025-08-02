using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace SpaceBlog.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CommentsController> _logger;

        public CommentsController(ApplicationDbContext context, ILogger<CommentsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Comments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetComments(
            [FromQuery] CommentStatus? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Article)
                    .Include(c => c.ModeratedBy)
                    .AsQueryable();

                // Фильтрация по статусу
                if (status.HasValue)
                {
                    switch (status.Value)
                    {
                        case CommentStatus.Pending:
                            query = query.Where(c => !c.IsApproved && !c.IsBlocked);
                            break;
                        case CommentStatus.Approved:
                            query = query.Where(c => c.IsApproved);
                            break;
                        case CommentStatus.Blocked:
                            query = query.Where(c => c.IsBlocked);
                            break;
                    }
                }

                var totalCount = await query.CountAsync();

                var comments = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new CommentResponseDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsApproved = c.IsApproved,
                        IsBlocked = c.IsBlocked,
                        ModeratedAt = c.ModeratedAt,
                        BlockReason = c.BlockReason,
                        ParentCommentId = c.ParentCommentId,
                        Status = c.Status,
                        Author = new CommentAuthorDto
                        {
                            Id = c.Author!.Id,
                            DisplayName = c.Author.GetDisplayName(),
                            AvatarUrl = c.Author.AvatarUrl
                        },
                        Article = new CommentArticleDto
                        {
                            Id = c.Article!.Id,
                            Title = c.Article.Title,
                            Slug = c.Article.Slug
                        },
                        ModeratedBy = c.ModeratedBy != null ? new ModeratedByDto
                        {
                            Id = c.ModeratedBy.Id,
                            DisplayName = c.ModeratedBy.GetDisplayName()
                        } : null,
                        RepliesCount = c.Replies.Count(r => r.IsApproved)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Comments = comments,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка комментариев");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Comments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CommentResponseDto>> GetComment(int id)
        {
            try
            {
                var comment = await _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Article)
                    .Include(c => c.ModeratedBy)
                    .Include(c => c.Replies.Where(r => r.IsApproved))
                        .ThenInclude(r => r.Author)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (comment == null)
                {
                    return NotFound("Комментарий не найден");
                }

                var commentDto = new CommentResponseDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    UpdatedAt = comment.UpdatedAt,
                    IsApproved = comment.IsApproved,
                    IsBlocked = comment.IsBlocked,
                    ModeratedAt = comment.ModeratedAt,
                    BlockReason = comment.BlockReason,
                    IpAddress = comment.IpAddress,
                    UserAgent = comment.UserAgent,
                    ParentCommentId = comment.ParentCommentId,
                    Status = comment.Status,
                    Author = new CommentAuthorDto
                    {
                        Id = comment.Author!.Id,
                        DisplayName = comment.Author.GetDisplayName(),
                        AvatarUrl = comment.Author.AvatarUrl
                    },
                    Article = new CommentArticleDto
                    {
                        Id = comment.Article!.Id,
                        Title = comment.Article.Title,
                        Slug = comment.Article.Slug
                    },
                    ModeratedBy = comment.ModeratedBy != null ? new ModeratedByDto
                    {
                        Id = comment.ModeratedBy.Id,
                        DisplayName = comment.ModeratedBy.GetDisplayName()
                    } : null,
                    Replies = comment.Replies.Select(r => new CommentResponseDto
                    {
                        Id = r.Id,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        IsApproved = r.IsApproved,
                        Author = new CommentAuthorDto
                        {
                            Id = r.Author!.Id,
                            DisplayName = r.Author.GetDisplayName(),
                            AvatarUrl = r.Author.AvatarUrl
                        }
                    }).ToList(),
                    RepliesCount = comment.Replies.Count(r => r.IsApproved)
                };

                return Ok(commentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении комментария {CommentId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // GET: api/Comments/article/{articleId}
        [HttpGet("article/{articleId}")]
        public async Task<ActionResult<IEnumerable<CommentResponseDto>>> GetArticleComments(
            int articleId,
            [FromQuery] bool includeReplies = true,
            [FromQuery] bool onlyApproved = true)
        {
            try
            {
                var article = await _context.Articles.FindAsync(articleId);
                if (article == null)
                {
                    return NotFound("Статья не найдена");
                }

                var query = _context.Comments
                    .Include(c => c.Author)
                    .Include(c => c.Replies.Where(r => !onlyApproved || r.IsApproved))
                        .ThenInclude(r => r.Author)
                    .Where(c => c.ArticleId == articleId && c.ParentCommentId == null);

                if (onlyApproved)
                {
                    query = query.Where(c => c.IsApproved);
                }

                var comments = await query
                    .OrderBy(c => c.CreatedAt)
                    .Select(c => new CommentResponseDto
                    {
                        Id = c.Id,
                        Content = c.Content,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        IsApproved = c.IsApproved,
                        IsBlocked = c.IsBlocked,
                        Status = c.Status,
                        Author = new CommentAuthorDto
                        {
                            Id = c.Author!.Id,
                            DisplayName = c.Author.GetDisplayName(),
                            AvatarUrl = c.Author.AvatarUrl
                        },
                        Replies = includeReplies ? c.Replies.Select(r => new CommentResponseDto
                        {
                            Id = r.Id,
                            Content = r.Content,
                            CreatedAt = r.CreatedAt,
                            UpdatedAt = r.UpdatedAt,
                            IsApproved = r.IsApproved,
                            Author = new CommentAuthorDto
                            {
                                Id = r.Author!.Id,
                                DisplayName = r.Author.GetDisplayName(),
                                AvatarUrl = r.Author.AvatarUrl
                            }
                        }).ToList() : new List<CommentResponseDto>(),
                        RepliesCount = c.Replies.Count(r => r.IsApproved)
                    })
                    .ToListAsync();

                return Ok(comments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении комментариев статьи {ArticleId}", articleId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Comments
        [HttpPost]
        public async Task<ActionResult<CommentResponseDto>> CreateComment(CreateCommentDto createDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Проверяем существование статьи
                var article = await _context.Articles.FindAsync(createDto.ArticleId);
                if (article == null)
                {
                    return BadRequest("Статья не найдена");
                }

                // Проверяем существование автора
                var author = await _context.Users.FindAsync(createDto.AuthorId);
                if (author == null)
                {
                    return BadRequest("Автор не найден");
                }

                // Проверяем родительский комментарий, если указан
                if (createDto.ParentCommentId.HasValue)
                {
                    var parentComment = await _context.Comments
                        .FirstOrDefaultAsync(c => c.Id == createDto.ParentCommentId.Value && c.ArticleId == createDto.ArticleId);
                    
                    if (parentComment == null)
                    {
                        return BadRequest("Родительский комментарий не найден");
                    }
                }

                var comment = new Comment
                {
                    Content = createDto.Content,
                    AuthorId = createDto.AuthorId,
                    ArticleId = createDto.ArticleId,
                    ParentCommentId = createDto.ParentCommentId,
                    IpAddress = createDto.IpAddress,
                    UserAgent = createDto.UserAgent,
                    CreatedAt = DateTime.Now,
                    IsApproved = false, // Комментарии требуют модерации
                    IsBlocked = false
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Создан комментарий: {CommentId} к статье {ArticleId} пользователем {AuthorId}", 
                    comment.Id, createDto.ArticleId, createDto.AuthorId);

                var commentDto = new CommentResponseDto
                {
                    Id = comment.Id,
                    Content = comment.Content,
                    CreatedAt = comment.CreatedAt,
                    IsApproved = comment.IsApproved,
                    IsBlocked = comment.IsBlocked,
                    ParentCommentId = comment.ParentCommentId,
                    Status = comment.Status,
                    Author = new CommentAuthorDto
                    {
                        Id = author.Id,
                        DisplayName = author.GetDisplayName(),
                        AvatarUrl = author.AvatarUrl
                    }
                };

                return CreatedAtAction(nameof(GetComment), new { id = comment.Id }, commentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании комментария");
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // PUT: api/Comments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, UpdateCommentDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    return NotFound("Комментарий не найден");
                }

                // Проверяем права на редактирование
                if (!comment.CanBeEditedBy(updateDto.UserId))
                {
                    return BadRequest("Время редактирования комментария истекло (15 минут)");
                }

                comment.UpdateContent(updateDto.Content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Обновлен комментарий: {CommentId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении комментария {CommentId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // DELETE: api/Comments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id, [FromQuery] string userId, [FromQuery] string userRoles = "")
        {
            try
            {
                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    return NotFound("Комментарий не найден");
                }

                // Проверяем права на удаление (упрощенная версия)
                var roles = userRoles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (!comment.CanBeDeletedBy(userId, roles))
                {
                    return Forbid("Недостаточно прав для удаления комментария");
                }

                _context.Comments.Remove(comment);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Удален комментарий: {CommentId} пользователем {UserId}", id, userId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении комментария {CommentId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Comments/{id}/approve
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveComment(int id, [FromQuery] string moderatorId)
        {
            try
            {
                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    return NotFound("Комментарий не найден");
                }

                comment.Approve(moderatorId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Одобрен комментарий: {CommentId} модератором {ModeratorId}", id, moderatorId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при одобрении комментария {CommentId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        // POST: api/Comments/{id}/block
        [HttpPost("{id}/block")]
        public async Task<IActionResult> BlockComment(int id, BlockCommentDto blockDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var comment = await _context.Comments.FindAsync(id);
                if (comment == null)
                {
                    return NotFound("Комментарий не найден");
                }

                comment.Block(blockDto.ModeratorId, blockDto.Reason);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Заблокирован комментарий: {CommentId} модератором {ModeratorId}, причина: {Reason}", 
                    id, blockDto.ModeratorId, blockDto.Reason);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при блокировке комментария {CommentId}", id);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }
    }

    // DTO классы для комментариев
    public class CommentResponseDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsApproved { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime? ModeratedAt { get; set; }
        public string? BlockReason { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public int? ParentCommentId { get; set; }
        public CommentStatus Status { get; set; }
        public CommentAuthorDto? Author { get; set; }
        public CommentArticleDto? Article { get; set; }
        public ModeratedByDto? ModeratedBy { get; set; }
        public List<CommentResponseDto> Replies { get; set; } = new();
        public int RepliesCount { get; set; }
    }

    public class CreateCommentDto
    {
        [Required(ErrorMessage = "Содержание комментария обязательно")]
        [StringLength(1000, ErrorMessage = "Комментарий не может быть длиннее 1000 символов")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Автор обязателен")]
        public string AuthorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Статья обязательна")]
        public int ArticleId { get; set; }

        public int? ParentCommentId { get; set; }

        [StringLength(45)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }
    }

    public class UpdateCommentDto
    {
        [Required(ErrorMessage = "Содержание комментария обязательно")]
        [StringLength(1000, ErrorMessage = "Комментарий не может быть длиннее 1000 символов")]
        public string Content { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пользователь обязателен")]
        public string UserId { get; set; } = string.Empty;
    }

    public class BlockCommentDto
    {
        [Required(ErrorMessage = "Модератор обязателен")]
        public string ModeratorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Причина блокировки обязательна")]
        [StringLength(500, ErrorMessage = "Причина блокировки не может быть длиннее 500 символов")]
        public string Reason { get; set; } = string.Empty;
    }

    public class CommentAuthorDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
    }

    public class CommentArticleDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Slug { get; set; }
    }

    public class ModeratedByDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}