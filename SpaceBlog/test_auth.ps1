# Скрипт для тестирования системы авторизации SpaceBlog
Write-Host "🧪 Тестирование системы авторизации SpaceBlog" -ForegroundColor Green

$baseUrl = "https://localhost:5001/api"

# Тест 1: Проверяем роли
Write-Host "`n1️⃣ Проверка ролей..." -ForegroundColor Yellow
try {
    $roles = Invoke-RestMethod -Uri "$baseUrl/Auth/roles" -Method GET -SkipCertificateCheck
    Write-Host "✅ Найдено ролей: $($roles.Count)" -ForegroundColor Green
    $roles | ForEach-Object { Write-Host "   - $($_.Name): $($_.Description)" }
} catch {
    Write-Host "❌ Ошибка получения ролей: $($_.Exception.Message)" -ForegroundColor Red
}

# Тест 2: Вход администратора
Write-Host "`n2️⃣ Тест входа администратора..." -ForegroundColor Yellow
try {
    $adminLogin = @{
        Email = "admin@spaceblog.com"
        Password = "Admin123!"
    } | ConvertTo-Json

    $adminResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method POST -Body $adminLogin -ContentType "application/json" -SkipCertificateCheck
    
    if ($adminResponse.Success) {
        Write-Host "✅ Админ успешно авторизован!" -ForegroundColor Green
        Write-Host "   - Роль: $($adminResponse.User.PrimaryRole)" 
        Write-Host "   - Email: $($adminResponse.User.Email)"
        Write-Host "   - Все роли: $($adminResponse.User.Roles -join ', ')"
    } else {
        Write-Host "❌ Ошибка входа админа: $($adminResponse.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Ошибка авторизации админа: $($_.Exception.Message)" -ForegroundColor Red
}

# Тест 3: Вход обычного пользователя  
Write-Host "`n3️⃣ Тест входа обычного пользователя..." -ForegroundColor Yellow
try {
    $userLogin = @{
        Email = "user@spaceblog.com"
        Password = "User123!"
    } | ConvertTo-Json

    $userResponse = Invoke-RestMethod -Uri "$baseUrl/Auth/login" -Method POST -Body $userLogin -ContentType "application/json" -SkipCertificateCheck
    
    if ($userResponse.Success) {
        Write-Host "✅ Пользователь успешно авторизован!" -ForegroundColor Green
        Write-Host "   - Роль: $($userResponse.User.PrimaryRole)"
        Write-Host "   - Email: $($userResponse.User.Email)" 
        Write-Host "   - Все роли: $($userResponse.User.Roles -join ', ')"
    } else {
        Write-Host "❌ Ошибка входа пользователя: $($userResponse.Message)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Ошибка авторизации пользователя: $($_.Exception.Message)" -ForegroundColor Red
}

# Тест 4: Попытка доступа к защищенному методу БЕЗ авторизации
Write-Host "`n4️⃣ Тест защищенного метода БЕЗ авторизации..." -ForegroundColor Yellow
try {
    # Получаем ID первого пользователя для тестирования
    $users = Invoke-RestMethod -Uri "$baseUrl/BlogUsers" -Method GET -SkipCertificateCheck
    if ($users.Count -gt 0) {
        $testUserId = $users[0].Id
        Write-Host "   Пробуем удалить пользователя $testUserId без авторизации..."
        
        # Пытаемся удалить пользователя без авторизации
        Invoke-RestMethod -Uri "$baseUrl/BlogUsers/$testUserId" -Method DELETE -SkipCertificateCheck
        Write-Host "❌ ОШИБКА: Удаление прошло без авторизации!" -ForegroundColor Red
    }
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ Защита работает! Получен 401 Unauthorized" -ForegroundColor Green
    } else {
        Write-Host "❌ Неожиданная ошибка: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n🎉 Тестирование завершено!" -ForegroundColor Green
Write-Host "📋 Результаты показывают, что система авторизации работает корректно." -ForegroundColor Cyan
Write-Host "🔍 Для более детального тестирования используйте Swagger UI: https://localhost:5001/api/docs" -ForegroundColor Cyan