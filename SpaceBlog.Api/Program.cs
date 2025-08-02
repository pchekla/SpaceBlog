using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Api.Data;
using SpaceBlog.Api.Models;
using SpaceBlog.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Автоматически определяем тип базы данных по строке подключения
    if (connectionString.Contains("Data Source=") && connectionString.EndsWith(".db"))
    {
        // SQLite
        options.UseSqlite(connectionString);
    }
    else if (connectionString.Contains("Server=") && (connectionString.Contains("SQLEXPRESS") || connectionString.Contains("Trusted_Connection")))
    {
        // SQL Server
        options.UseSqlServer(connectionString);
    }
    else
    {
        // MySQL - используем фиксированную версию для избежания проблем с автоопределением при недоступности сервера
        var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
        options.UseMySql(connectionString, serverVersion);
    }
});

// Настройка Identity с ролями (для API аутентификации)
builder.Services.AddIdentity<BlogUser, Role>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Добавляем поддержку Web API контроллеров
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Используем PascalCase
        options.JsonSerializerOptions.WriteIndented = true; // Красивое форматирование JSON
    });

// Добавляем Swagger для документации API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "SpaceBlog API",
        Version = "v1",
        Description = "API для управления блогом SpaceBlog"
    });
});

// Настройка авторизации с политиками
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministrator", policy =>
        policy.RequireRole(Role.Names.Administrator));
    
    options.AddPolicy("RequireModerator", policy =>
        policy.RequireRole(Role.Names.Moderator, Role.Names.Administrator));
    
    options.AddPolicy("RequireAuthor", policy =>
        policy.RequireRole(Role.Names.Author, Role.Names.Moderator, Role.Names.Administrator));
    
    options.AddPolicy("RequireUser", policy =>
        policy.RequireAuthenticatedUser());
});

// Настройка CORS для фронтенда
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Глобальный обработчик исключений для API
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpaceBlog API V1");
        c.RoutePrefix = "swagger"; // Swagger UI будет доступен по адресу /swagger
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Инициализация базы данных и данных (только если это необходимо)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Проверка подключения к базе данных
        logger.LogInformation("API: Проверка подключения к базе данных...");
        await context.Database.CanConnectAsync();
        logger.LogInformation("API: Подключение к базе данных успешно.");
        
        // Применение миграций (если необходимо)
        if (context.Database.GetPendingMigrations().Any())
        {
            logger.LogInformation("API: Применение миграций...");
            await context.Database.MigrateAsync();
        }
        
        logger.LogInformation("API: Инициализация базы данных завершена успешно.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "API: Ошибка при инициализации базы данных");
        logger.LogWarning("API: Приложение продолжит работу, но без доступа к базе данных.");
    }
}

// Настройка маршрутов для API контроллеров
app.MapControllers();

app.Run();