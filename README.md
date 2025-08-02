# 🚀 SpaceBlog

**Современная платформа для ведения блога с космической тематикой**

[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-blue.svg)](https://docs.microsoft.com/en-us/aspnet/core/)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-green.svg)](https://docs.microsoft.com/en-us/ef/core/)
[![Bootstrap](https://img.shields.io/badge/Bootstrap-5.3-purple.svg)](https://getbootstrap.com/)

## 📖 Описание

SpaceBlog - это полнофункциональная платформа для ведения блога, построенная на ASP.NET Core с использованием современных веб-технологий. Проект разделен на два независимых компонента: веб-приложение с Razor Pages и отдельный REST API. Система включает в себя управление пользователями, создание и редактирование статей, комментарии, теги и многое другое.

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
- **Раздельная архитектура** для повышения безопасности

### 🏗️ Архитектура
- **Два независимых проекта**: веб-приложение и REST API
- **Разделение ответственности** между UI и API
- **Модульная структура** для легкого масштабирования
- **Независимое развертывание** компонентов
- **API-first подход** для будущих мобильных приложений
- **Лучшая производительность** за счет оптимизации каждого компонента
- **Простота тестирования** отдельных компонентов
- **Возможность создания SPA** фронтенда в будущем

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
- **Razor Pages** - Для серверного рендеринга (веб-проект)
- **Web API** - REST API для программного доступа (API проект)
- **Swagger/OpenAPI** - Документация API

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
   git clone https://github.com/pchekla/SpaceBlog.git
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

3. **Запуск приложений**
   
   **Запуск веб-приложения:**
   ```bash
   cd SpaceBlog
   dotnet run
   ```
   
   **Запуск API (в отдельном терминале):**
   ```bash
   cd SpaceBlog.Api
   dotnet run
   ```

4. **Открытие в браузере**
   
   **Веб-приложение:**
   ```
   https://localhost:5001
   ```
   
   **API документация (Swagger):**
   ```
   https://localhost:7001/swagger
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
SpaceBlog/                    # 🌐 Веб-приложение (Razor Pages)
├── 📁 Areas/
│   └── Identity/            # Страницы аутентификации
├── 📁 Pages/               # Razor Pages
│   ├── Articles/           # Управление статьями
│   ├── Users/             # Управление пользователями
│   ├── Tags/              # Управление тегами
│   ├── Admin/             # Административная панель
│   ├── Error/             # Страницы ошибок
│   └── Shared/            # Общие компоненты
├── 📁 Properties/          # Настройки проекта
├── 📁 wwwroot/            # Статические файлы
└── 📄 Program.cs          # Конфигурация веб-приложения

SpaceBlog.Api/               # 🚀 REST API проект
├── 📁 Controllers/         # API контроллеры
├── 📁 Data/               # Контекст БД и сидинг данных
├── 📁 Middleware/         # Глобальный обработчик исключений
├── 📁 Models/             # Модели данных
├── 📁 Properties/         # Настройки API проекта
└── 📄 Program.cs          # Конфигурация API и Swagger

SpaceBlog.sln               # 📦 Solution файл
```

## 🔧 Основные возможности

### Система ролей

- **Administrator** - Полный доступ ко всем функциям системы
- **Moderator** - Модерация контента, управление пользователями
- **Author** - Создание и редактирование собственных статей
- **User** - Чтение статей, комментирование

### API Endpoints

**Документация API доступна по адресу:** `https://localhost:7001/swagger` (Swagger UI)

**Основные эндпоинты:**
- **`GET/POST/PUT/DELETE /api/articles`** - Управление статьями
- **`GET/POST/PUT/DELETE /api/users`** - Управление пользователями  
- **`GET/POST/PUT/DELETE /api/comments`** - Управление комментариями
- **`GET/POST/PUT/DELETE /api/tags`** - Управление тегами
- **`POST /api/auth/login`** - Аутентификация пользователей
- **`POST /api/auth/register`** - Регистрация новых пользователей

**Особенности API:**
- 🔒 **JWT аутентификация** для защищенных эндпоинтов
- 📝 **Автоматическая документация** через Swagger/OpenAPI
- 🚀 **CORS поддержка** для фронтенд приложений
- ⚡ **Быстрые JSON ответы** без лишней разметки

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

**Общие команды:**
```bash
# Сборка всего решения
dotnet build

# Очистка и пересборка
dotnet clean && dotnet build
```

**Веб-приложение (SpaceBlog/):**
```bash
# Запуск веб-приложения
cd SpaceBlog
dotnet run

# Сборка только веб-проекта
dotnet build
```

**API проект (SpaceBlog.Api/):**
```bash
# Запуск API сервера
cd SpaceBlog.Api
dotnet run

# Сборка только API проекта
dotnet build
```

**Работа с базой данных:**
```bash
# Создание миграции (из любого проекта)
dotnet ef migrations add MigrationName --project SpaceBlog.Api

# Применение миграций (из любого проекта)
dotnet ef database update --project SpaceBlog.Api

# Удаление последней миграции
dotnet ef migrations remove --project SpaceBlog.Api
```

## 🚀 Развертывание

### Режимы развертывания

**1. Монолитное развертывание (рекомендуется для начала):**
- Запускайте оба проекта на одном сервере
- Веб-приложение: `https://yourdomain.com`
- API: `https://yourdomain.com:7001` или `https://api.yourdomain.com`

**2. Микросервисное развертывание:**
- Веб-приложение на отдельном сервере/контейнере
- API на отдельном сервере/контейнере  
- Возможность независимого масштабирования

### Docker развертывание

**Создание образов:**
```bash
# Сборка веб-приложения
docker build -f SpaceBlog/Dockerfile -t spaceblog-web .

# Сборка API
docker build -f SpaceBlog.Api/Dockerfile -t spaceblog-api .
```

**Docker Compose (рекомендуется):**
```yaml
version: '3.8'
services:
  web:
    image: spaceblog-web
    ports:
      - "5001:5001"
    depends_on:
      - api
      - database
  
  api:
    image: spaceblog-api  
    ports:
      - "7001:7001"
    depends_on:
      - database
      
  database:
    image: postgres:15
    environment:
      POSTGRES_DB: spaceblog
      POSTGRES_USER: user
      POSTGRES_PASSWORD: password
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

- 🐛 [Создайте Issue](https://github.com/pchekla/SpaceBlog/issues)
- 💬 [Обсуждения](https://github.com/pchekla/SpaceBlog/discussions)
- 📧 Email: support@spaceBlog.com

---

<div align="center">

**Сделано с ❤️ для сообщества разработчиков**

[⬆ Наверх](#-SpaceBlog)

</div>