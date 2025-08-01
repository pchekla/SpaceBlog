using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Data;
using SpaceBlog.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=SpaceBlog.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<BlogUser>(options => 
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<Role>() // Добавляем поддержку ролей
    .AddEntityFrameworkStores<ApplicationDbContext>();

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

builder.Services.AddRazorPages();

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

// Инициализация данных
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SpaceBlog.Data.SeedData.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка при инициализации данных");
    }
}

// Настройка маршрутов для API контроллеров
app.MapControllers();
app.MapRazorPages();

app.Run();
