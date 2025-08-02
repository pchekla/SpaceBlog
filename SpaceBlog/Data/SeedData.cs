using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpaceBlog.Models;

namespace SpaceBlog.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            var userManager = serviceProvider.GetRequiredService<UserManager<BlogUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<Role>>();

        // Создаем роли если они не существуют
        await CreateRolesAsync(roleManager);

        // Создаем администратора если он не существует
        await CreateAdminUserAsync(userManager);

        // Создаем тестовых пользователей если их нет
        await CreateTestUsersAsync(userManager);

        // Проверяем и исправляем существующих тестовых пользователей
        await FixExistingTestUsersAsync(userManager);

        // Создаем тестовые данные
        await CreateSampleDataAsync(context, userManager);
        }

        private static async Task CreateRolesAsync(RoleManager<Role> roleManager)
        {
            string[] roleNames = { Role.Names.Administrator, Role.Names.Moderator, Role.Names.Author, Role.Names.User };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var role = new Role
                    {
                        Name = roleName,
                        Description = GetRoleDescription(roleName),
                        Priority = GetRolePriority(roleName)
                    };
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task CreateAdminUserAsync(UserManager<BlogUser> userManager)
        {
            var adminEmail = "admin@spaceblog.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new BlogUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Администратор",
                    LastName = "1",
                    Bio = "Главный администратор блога SpaceBlog",
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Role.Names.Administrator);
                }
            }
        }

        private static async Task CreateTestUsersAsync(UserManager<BlogUser> userManager)
        {
            // Создаем тестового модератора
            var moderatorEmail = "moderator@spaceblog.com";
            var moderatorUser = await userManager.FindByEmailAsync(moderatorEmail);

            if (moderatorUser == null)
            {
                moderatorUser = new BlogUser
                {
                    UserName = moderatorEmail,
                    Email = moderatorEmail,
                    EmailConfirmed = true,
                    FirstName = "Модератор",
                    LastName = "1",
                    Bio = "Модератор контента в космическом блоге",
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(moderatorUser, "Moderator123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(moderatorUser, Role.Names.Moderator);
                    Console.WriteLine($"✅ Тестовый модератор создан: {moderatorEmail}");
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка создания модератора: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️ Тестовый модератор уже существует: {moderatorEmail}");
            }

            // Создаем тестового пользователя
            var userEmail = "user@spaceblog.com";
            var testUser = await userManager.FindByEmailAsync(userEmail);

            if (testUser == null)
            {
                testUser = new BlogUser
                {
                    UserName = userEmail,
                    Email = userEmail,
                    EmailConfirmed = true,
                    FirstName = "Пользователь",
                    LastName = "1",
                    Bio = "Обычный пользователь блога, читатель статей",
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(testUser, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(testUser, Role.Names.User);
                    Console.WriteLine($"✅ Тестовый пользователь создан: {userEmail}");
                }
                else
                {
                    Console.WriteLine($"❌ Ошибка создания пользователя: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                Console.WriteLine($"ℹ️ Тестовый пользователь уже существует: {userEmail}");
            }
        }

        private static async Task FixExistingTestUsersAsync(UserManager<BlogUser> userManager)
        {
            Console.WriteLine("🔧 Проверяем существующих тестовых пользователей...");
            
            // Проверяем модератора
            var moderatorUser = await userManager.FindByEmailAsync("moderator@spaceblog.com");
            if (moderatorUser != null)
            {
                // Проверяем пароль
                var passwordCheck = await userManager.CheckPasswordAsync(moderatorUser, "Moderator123!");
                if (!passwordCheck)
                {
                    Console.WriteLine("🔑 Исправляем пароль модератора...");
                    var token = await userManager.GeneratePasswordResetTokenAsync(moderatorUser);
                    await userManager.ResetPasswordAsync(moderatorUser, token, "Moderator123!");
                }
                
                // Проверяем роль
                var isInRole = await userManager.IsInRoleAsync(moderatorUser, Role.Names.Moderator);
                if (!isInRole)
                {
                    Console.WriteLine("👑 Добавляем роль модератора...");
                    await userManager.AddToRoleAsync(moderatorUser, Role.Names.Moderator);
                }
            }
            
            // Проверяем обычного пользователя
            var testUser = await userManager.FindByEmailAsync("user@spaceblog.com");
            if (testUser != null)
            {
                // Проверяем пароль
                var passwordCheck = await userManager.CheckPasswordAsync(testUser, "User123!");
                if (!passwordCheck)
                {
                    Console.WriteLine("🔑 Исправляем пароль пользователя...");
                    var token = await userManager.GeneratePasswordResetTokenAsync(testUser);
                    await userManager.ResetPasswordAsync(testUser, token, "User123!");
                }
                
                // Проверяем роль
                var isInRole = await userManager.IsInRoleAsync(testUser, Role.Names.User);
                if (!isInRole)
                {
                    Console.WriteLine("👤 Добавляем роль пользователя...");
                    await userManager.AddToRoleAsync(testUser, Role.Names.User);
                }
            }
        }

        private static async Task CreateSampleDataAsync(ApplicationDbContext context, UserManager<BlogUser> userManager)
        {
            // Создаем тестового автора если его нет
            var authorEmail = "author@spaceblog.com";
            var authorUser = await userManager.FindByEmailAsync(authorEmail);

            if (authorUser == null)
            {
                authorUser = new BlogUser
                {
                    UserName = authorEmail,
                    Email = authorEmail,
                    EmailConfirmed = true,
                    FirstName = "Тестовый",
                    LastName = "Автор",
                    Bio = "Автор тестовых статей для демонстрации возможностей блога",
                    RegistrationDate = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(authorUser, "Author123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(authorUser, Role.Names.Author);
                }
            }

            // Создаем теги если их нет
            if (!context.Tags.Any())
            {
                var tags = new[]
                {
                    new Tag { Name = "Космос", Slug = "cosmos", Description = "Статьи о космосе и астрономии", Color = "#1e3a8a" },
                    new Tag { Name = "Технологии", Slug = "tech", Description = "Новые технологии и разработки", Color = "#059669" },
                    new Tag { Name = "Наука", Slug = "science", Description = "Научные открытия и исследования", Color = "#dc2626" },
                    new Tag { Name = "Программирование", Slug = "programming", Description = "Разработка ПО и программирование", Color = "#7c3aed" },
                    new Tag { Name = "Искусственный интеллект", Slug = "ai", Description = "ИИ и машинное обучение", Color = "#ea580c" }
                };

                context.Tags.AddRange(tags);
                await context.SaveChangesAsync();
            }

            // Создаем статьи если их нет
            if (!context.Articles.Any())
            {
                var tags = await context.Tags.ToListAsync();
                
                var articles = new[]
                {
                    new Article
                    {
                        Title = "Добро пожаловать в SpaceBlog!",
                        Slug = "welcome-to-spaceblog",
                        Summary = "Первая статья в нашем космическом блоге",
                        Content = "<h2>Добро пожаловать!</h2><p>Это первая статья в нашем блоге о космосе, технологиях и науке. Здесь мы будем делиться интересными фактами, новостями и исследованиями.</p>",
                        AuthorId = authorUser!.Id,
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-10),
                        ViewCount = 150
                    },
                    new Article
                    {
                        Title = "Исследование Марса: новые горизонты",
                        Slug = "mars-exploration-new-horizons",
                        Summary = "Обзор последних достижений в исследовании красной планеты",
                        Content = "<h2>Красная планета</h2><p>Марс продолжает удивлять ученых своими тайнами. В этой статье мы рассмотрим последние открытия...</p>",
                        AuthorId = authorUser!.Id,
                        IsPublished = true,
                        CreatedAt = DateTime.UtcNow.AddDays(-5),
                        ViewCount = 89
                    }
                };

                context.Articles.AddRange(articles);
                await context.SaveChangesAsync();

                // Добавляем теги к статьям
                var article1 = articles[0];
                var article2 = articles[1];
                
                context.ArticleTags.AddRange(
                    new ArticleTag { ArticleId = article1.Id, TagId = tags.First(t => t.Slug == "cosmos").Id },
                    new ArticleTag { ArticleId = article1.Id, TagId = tags.First(t => t.Slug == "tech").Id },
                    new ArticleTag { ArticleId = article2.Id, TagId = tags.First(t => t.Slug == "cosmos").Id },
                    new ArticleTag { ArticleId = article2.Id, TagId = tags.First(t => t.Slug == "science").Id }
                );

                await context.SaveChangesAsync();
            }
        }

        private static string GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                Role.Names.Administrator => "Полные права администратора системы",
                Role.Names.Moderator => "Права модератора для управления контентом",
                Role.Names.Author => "Права автора для создания статей",
                Role.Names.User => "Стандартные права пользователя",
                _ => "Пользовательская роль"
            };
        }

        private static int GetRolePriority(string roleName)
        {
            return roleName switch
            {
                Role.Names.Administrator => 100,
                Role.Names.Moderator => 75,
                Role.Names.Author => 50,
                Role.Names.User => 25,
                _ => 0
            };
        }
    }
}