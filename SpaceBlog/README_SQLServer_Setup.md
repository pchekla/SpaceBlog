# Настройка SQL Server для SpaceBlog

## Настройка для SQL Server Express

Если у вас установлен SQL Server Express (как показано на скриншотах), используйте следующую конфигурацию:

### Шаг 1: Обновите appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=SpaceBlog;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### Шаг 2: Создайте базу данных (опционально)

Приложение автоматически создаст базу данных при первом запуске, но вы также можете создать её вручную:

1. Откройте SQL Server Management Studio (SSMS)
2. Подключитесь к `.\SQLEXPRESS`
3. Выполните команду:
   ```sql
   CREATE DATABASE SpaceBlog;
   ```

### Шаг 3: Запустите приложение

```bash
dotnet run
```

## Альтернативные строки подключения

### Для именованного экземпляра SQL Server:
```json
"DefaultConnection": "Server=.\\ИМЯ_ЭКЗЕМПЛЯРА;Database=SpaceBlog;Trusted_Connection=true;TrustServerCertificate=true;"
```

### Для SQL Server с аутентификацией:
```json
"DefaultConnection": "Server=localhost;Database=SpaceBlog;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
```

### Для удаленного SQL Server:
```json
"DefaultConnection": "Server=server_name;Database=SpaceBlog;User Id=username;Password=password;TrustServerCertificate=true;"
```

## Проверка работы

После запуска `dotnet run` вы должны увидеть сообщения:
- "Проверка подключения к базе данных..."
- "Подключение к базе данных успешно."
- "Создание базы данных если не существует..."
- "Применение миграций..."
- "Инициализация тестовых данных..."
- "Инициализация базы данных завершена успешно."

## Тестовые пользователи

После первого запуска будут созданы:
- **admin@spaceblog.ru** / Admin123! (Администратор)
- **moderator@spaceblog.ru** / Moderator123! (Модератор)  
- **author@spaceblog.ru** / Author123! (Автор)
- **user@spaceblog.ru** / User123! (Пользователь)

## Устранение проблем

### Ошибка "Невозможно подключиться к SQL Server"
1. Убедитесь, что SQL Server Express запущен
2. Проверьте имя экземпляра в строке подключения
3. Убедитесь, что TCP/IP протокол включен в SQL Server Configuration Manager

### Ошибка "База данных не существует"
- Приложение автоматически создаст базу данных при первом запуске
- Убедитесь, что у пользователя есть права на создание баз данных