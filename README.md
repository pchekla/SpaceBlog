# 🚀 SpaceBlog

**Современная платформа для ведения блога с космической тематикой**

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue.svg)](https://docs.microsoft.com/en-us/aspnet/core/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-green.svg)](https://docs.microsoft.com/en-us/ef/core/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-purple.svg)](https://getbootstrap.com/)

## 📖 Описание

SpaceBlog - это полнофункциональная платформа для ведения блога, построенная на ASP.NET Core с использованием современных веб-технологий. Проект включает в себя систему управления пользователями, создание и редактирование статей, комментарии, теги и многое другое.

## ✨ Основные функции

### 👥 Управление пользователями
- **Регистрация и авторизация** с валидацией в реальном времени
- **Система ролей** (Administrator, Moderator, Author, User)
- **Профили пользователей** с возможностью редактирования
- **Управление пользователями** для администраторов

### 📝 Система статей
- **Создание и редактирование статей** с богатым текстовым редактором
- **Система тегов** для категоризации контента
- **Комментарии** к статьям
- **Личные статьи** пользователей
- **Публичный каталог** всех статей

### 🛡️ Безопасность
- **ASP.NET Core Identity** для аутентификации
- **Система авторизации** на основе ролей
- **Антифоджерные токены** для защиты от CSRF
- **Глобальный обработчик исключений** с логированием

### 🎨 Пользовательский интерфейс
- **Адаптивный дизайн** на Bootstrap 5
- **Красивые страницы ошибок** (404, 403, 500) с анимациями
- **Интерактивные элементы** и анимации
- **Космическая тематика** в дизайне

### 🗄️ База данных
- Поддержка **множественных СУБД**:
  - 🐘 PostgreSQL
  - 🐬 MySQL
  - 🗃️ SQL Server
  - 📱 SQLite
- **Entity Framework Core** для работы с данными
- **Автоматические миграции** и инициализация данных

## 🛠️ Технологический стек

### Backend
- **.NET 8.0** - Основная платформа
- **ASP.NET Core 8.0** - Веб-фреймворк
- **Entity Framework Core** - ORM для работы с базой данных
- **ASP.NET Core Identity** - Система аутентификации
- **Razor Pages** - Для серверного рендеринга

### Frontend
- **Bootstrap 5.3** - CSS-фреймворк
- **Font Awesome** - Иконки
- **jQuery** - JavaScript библиотека
- **Vanilla JavaScript** - Для интерактивности

### База данных
- **PostgreSQL** (рекомендуется)
- **MySQL** 
- **SQL Server**
- **SQLite** (для разработки)

## 🚀 Быстрый старт

### Предварительные требования

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- База данных (PostgreSQL/MySQL/SQL Server/SQLite)
- IDE (Visual Studio, VS Code, Rider)

### Установка и запуск

1. **Клонирование репозитория**
   ```bash
   git clone https://github.com/your-username/SpaceBlog.git
   cd SpaceBlog
   ```

2. **Настройка базы данных**
   
   Скопируйте файл настроек:
   ```bash
   cp SpaceBlog/appsettings.Example.json SpaceBlog/appsettings.json
   ```
   
   Отредактируйте строку подключения в `SpaceBlog/appsettings.json`:
   
   **Для PostgreSQL:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=SpaceBlog;Username=your_username;Password=your_password"
     }
   }
   ```
   
   **Для MySQL:**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=SpaceBlog;User=your_username;Password=your_password"
     }
   }
   ```
   
   **Для SQLite (для разработки):**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Data Source=SpaceBlog.db"
     }
   }
   ```

3. **Запуск приложения**
   ```bash
   cd SpaceBlog
   dotnet run
   ```

4. **Открытие в браузере**
   ```
   https://localhost:5257
   ```

## 📋 Детальная настройка базы данных

### PostgreSQL
См. подробные инструкции в [README_PostgreSQL_Setup.md](SpaceBlog/README_PostgreSQL_Setup.md)

### MySQL  
См. подробные инструкции в [README_MySQL_Setup.md](SpaceBlog/README_MySQL_Setup.md)

### SQL Server
См. подробные инструкции в [README_SQLServer_Setup.md](SpaceBlog/README_SQLServer_Setup.md)

## 👤 Учетные записи по умолчанию

После первого запуска автоматически создаются тестовые пользователи:

| Роль | Email | Пароль | Описание |
|------|-------|--------|----------|
| Administrator | admin@spaceBlog.com | Admin123! | Полный доступ ко всем функциям |
| Moderator | moderator@spaceBlog.com | Moderator123! | Модерация контента |
| Author | author@spaceBlog.com | Author123! | Создание статей |
| User | user@spaceBlog.com | User123! | Базовый пользователь |

## 🗺️ Структура проекта

```
SpaceBlog/
├── 📁 Areas/
│   └── Identity/          # Страницы аутентификации
├── 📁 Controllers/        # API контроллеры
├── 📁 Data/              # Контекст БД и сидинг данных
├── 📁 Middleware/        # Кастомные middleware
├── 📁 Models/            # Модели данных
├── 📁 Pages/             # Razor Pages
│   ├── Articles/         # Управление статьями
│   ├── Users/           # Управление пользователями
│   ├── Tags/            # Управление тегами
│   ├── Admin/           # Административная панель
│   ├── Error/           # Страницы ошибок
│   └── Shared/          # Общие компоненты
├── 📁 Properties/        # Настройки проекта
└── 📁 wwwroot/          # Статические файлы
```

## 🔧 Основные возможности

### Система ролей

- **Administrator** - Полный доступ ко всем функциям системы
- **Moderator** - Модерация контента, управление пользователями
- **Author** - Создание и редактирование собственных статей
- **User** - Чтение статей, комментирование

### API Endpoints

Документация API доступна по адресу: `/api/docs` (Swagger UI)

Основные эндпоинты:
- `/api/articles` - Управление статьями
- `/api/users` - Управление пользователями  
- `/api/comments` - Управление комментариями
- `/api/tags` - Управление тегами

### Обработка ошибок

Система включает комплексную обработку ошибок:

- **Глобальный middleware** для перехвата исключений
- **Красивые страницы ошибок** для 403, 404, 500
- **Логирование** всех ошибок и исключений
- **API-совместимые** JSON ответы для ошибок

### Тестирование

Для тестирования системы ошибок доступна страница: `/TestErrors`

## 🔐 Безопасность

- **HTTPS** принудительно включен
- **Anti-forgery tokens** для защиты от CSRF
- **XSS защита** через Razor Pages
- **SQL Injection защита** через Entity Framework
- **Валидация** на стороне сервера и клиента

## 📱 Адаптивность

Приложение полностью адаптивно и корректно работает на:
- 🖥️ Десктопах
- 💻 Планшетах  
- 📱 Мобильных устройствах

## 🎨 Кастомизация

### Темы
Приложение использует космическую цветовую схему, которую можно настроить в `wwwroot/css/site.css`

### Логотип
Замените иконку ракеты в навигационной панели на свой логотип

## 🚧 Разработка

### Требования для разработки
- Visual Studio 2022 или VS Code
- .NET 8.0 SDK
- Node.js (для фронтенд инструментов)

### Полезные команды

```bash
# Сборка проекта
dotnet build

# Запуск в режиме разработки
dotnet run

# Создание миграции
dotnet ef migrations add MigrationName

# Применение миграций
dotnet ef database update

# Очистка и пересборка
dotnet clean && dotnet build
```

## 📄 Лицензия

Этот проект распространяется под лицензией MIT. См. файл [LICENSE](LICENSE) для подробностей.

## 🤝 Вклад в проект

Мы приветствуем вклад в развитие проекта! Пожалуйста:

1. Форкните репозиторий
2. Создайте ветку для новой функции (`git checkout -b feature/AmazingFeature`)
3. Зафиксируйте изменения (`git commit -m 'Add some AmazingFeature'`)
4. Отправьте в ветку (`git push origin feature/AmazingFeature`)
5. Откройте Pull Request

## 📞 Поддержка

Если у вас есть вопросы или проблемы:

- 🐛 [Создайте Issue](https://github.com/your-username/SpaceBlog/issues)
- 💬 [Обсуждения](https://github.com/your-username/SpaceBlog/discussions)
- 📧 Email: support@spaceBlog.com

---

<div align="center">

**Сделано с ❤️ для сообщества разработчиков**

[⬆ Наверх](#-SpaceBlog)

</div>