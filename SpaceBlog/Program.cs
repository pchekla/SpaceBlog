using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

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
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Настройка Identity с ролями
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

builder.Services.AddRazorPages()
    .AddRazorRuntimeCompilation(); // Добавляем горячую перезагрузку Razor страниц

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SpaceBlog API V1");
        c.RoutePrefix = "api/docs"; // Swagger UI будет доступен по адресу /api/docs
    });
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Инициализация базы данных и данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Проверка подключения к базе данных
        logger.LogInformation("Проверка подключения к базе данных...");
        await context.Database.CanConnectAsync();
        logger.LogInformation("Подключение к базе данных успешно.");
        
        // Создание базы данных если она не существует
        logger.LogInformation("Создание базы данных если не существует...");
        await context.Database.EnsureCreatedAsync();
        
        // Применение миграций
        logger.LogInformation("Применение миграций...");
        await context.Database.MigrateAsync();
        
        // Инициализация тестовых данных
        logger.LogInformation("Инициализация тестовых данных...");
        await SpaceBlog.Data.SeedData.Initialize(services);
        
        logger.LogInformation("Инициализация базы данных завершена успешно.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при инициализации базы данных");
        logger.LogWarning("ВАЖНО: Проверьте настройки подключения к MySQL в appsettings.json");
        logger.LogWarning("Убедитесь что:");
        logger.LogWarning("1. MySQL Server запущен");
        logger.LogWarning("2. База данных создана");
        logger.LogWarning("3. Пользователь имеет права доступа");
        logger.LogWarning("4. Строка подключения корректна");
        logger.LogWarning("Приложение продолжит работу, но без доступа к базе данных.");
    }
}

// Настройка маршрутов для API контроллеров
app.MapControllers();
app.MapRazorPages();

app.Run();
