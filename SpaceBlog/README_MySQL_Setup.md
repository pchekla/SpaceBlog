# Настройка базы данных для SpaceBlog

Приложение поддерживает работу как с SQLite (по умолчанию), так и с MySQL.

## Быстрый старт (SQLite)

Приложение настроено на использование SQLite по умолчанию. Просто запустите:

```bash
dotnet run
```

База данных и тестовые данные создадутся автоматически.

## Настройка MySQL

### Требования
1. Установленный MySQL Server
2. Созданная база данных

## Настройка подключения

### 1. Скопируйте пример конфигурации
```bash
cp appsettings.Example.json appsettings.json
```

### 2. Обновите строку подключения в appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_mysql_server;Database=SpaceBlog;Uid=your_username;Pwd=your_password;"
  }
}
```

### 3. Для среды разработки обновите appsettings.Development.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SpaceBlog_Dev;Uid=root;Pwd=your_password;"
  }
}
```

## Создание базы данных в MySQL

Выполните следующие команды в MySQL:

```sql
CREATE DATABASE SpaceBlog CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE SpaceBlog_Dev CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

## Запуск приложения

После настройки строки подключения просто запустите:

```bash
dotnet run
```

Приложение автоматически:
- Создаст необходимые таблицы
- Применит миграции
- Создаст тестовые данные (пользователи, статьи, теги, комментарии)

## Тестовые пользователи

После первого запуска будут созданы следующие пользователи:

| Email | Пароль | Роль |
|-------|--------|------|
| admin@spaceblog.ru | Admin123! | Администратор |
| moderator@spaceblog.ru | Moderator123! | Модератор |
| author@spaceblog.ru | Author123! | Автор |
| user@spaceblog.ru | User123! | Пользователь |

## Возможные проблемы

### Ошибка подключения к MySQL
- Убедитесь, что MySQL Server запущен
- Проверьте правильность строки подключения
- Убедитесь, что база данных создана
- Проверьте права пользователя

### Ошибки миграций
Если возникли проблемы с миграциями, удалите папку Migrations и пересоздайте их:

```bash
Remove-Item -Recurse -Force Migrations
dotnet ef migrations add InitialCreate
dotnet ef database update
```