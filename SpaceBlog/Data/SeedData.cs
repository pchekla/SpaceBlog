using Microsoft.AspNetCore.Identity;
using SpaceBlog.Models;

namespace SpaceBlog.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<BlogUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();

            // Создаем роли
            await CreateRolesAsync(roleManager);

            // Создаем пользователей
            await CreateUsersAsync(userManager);
        }

        private static async Task CreateRolesAsync(RoleManager<Role> roleManager)
        {
            var roles = new[]
            {
                new Role
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Role.Names.Administrator,
                    NormalizedName = Role.Names.Administrator.ToUpper(),
                    Description = "Полные права администратора системы",
                    Priority = 100,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Role
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Role.Names.Moderator,
                    NormalizedName = Role.Names.Moderator.ToUpper(),
                    Description = "Права модератора контента",
                    Priority = 75,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Role
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Role.Names.Author,
                    NormalizedName = Role.Names.Author.ToUpper(),
                    Description = "Права автора статей",
                    Priority = 50,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                },
                new Role
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Role.Names.User,
                    NormalizedName = Role.Names.User.ToUpper(),
                    Description = "Базовые права пользователя",
                    Priority = 25,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                }
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role.Name!))
                {
                    await roleManager.CreateAsync(role);
                }
            }
        }

        private static async Task CreateUsersAsync(UserManager<BlogUser> userManager)
        {
            // Администратор
            var admin = new BlogUser
            {
                UserName = "admin@spaceblog.com",
                Email = "admin@spaceblog.com",
                FirstName = "Админ",
                LastName = "Админов",
                DisplayName = "Главный Администратор",
                Bio = "Администратор блога SpaceBlog",
                RegistrationDate = DateTime.Now,
                EmailNotifications = true,
                IsPublicProfile = true,
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(admin.Email) == null)
            {
                var result = await userManager.CreateAsync(admin, "Admin123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, Role.Names.Administrator);
                }
            }

            // Модератор
            var moderator = new BlogUser
            {
                UserName = "moderator@spaceblog.com",
                Email = "moderator@spaceblog.com",
                FirstName = "Модер",
                LastName = "Модеров",
                DisplayName = "Главный Модератор",
                Bio = "Модератор контента блога SpaceBlog",
                RegistrationDate = DateTime.Now,
                EmailNotifications = true,
                IsPublicProfile = true,
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(moderator.Email) == null)
            {
                var result = await userManager.CreateAsync(moderator, "Moderator123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(moderator, Role.Names.Moderator);
                }
            }

            // Обычный пользователь
            var user = new BlogUser
            {
                UserName = "user@spaceblog.com",
                Email = "user@spaceblog.com",
                FirstName = "Иван",
                LastName = "Иванов",
                DisplayName = "Иван Иванов",
                Bio = "Обычный пользователь блога SpaceBlog",
                RegistrationDate = DateTime.Now,
                EmailNotifications = true,
                IsPublicProfile = true,
                EmailConfirmed = true
            };

            if (await userManager.FindByEmailAsync(user.Email) == null)
            {
                var result = await userManager.CreateAsync(user, "User123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, Role.Names.User);
                }
            }
        }
    }
}