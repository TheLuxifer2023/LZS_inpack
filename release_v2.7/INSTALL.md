# 🚀 LZS Phyre Engine Tool v2.7 - Установка

## Системные требования

- **Windows 10/11** (x64)
- **.NET 6.0 Runtime** (для веб-интерфейса)
- **Минимум 50 МБ** свободного места

## Быстрая установка

### 1. Скачайте и установите .NET 6.0 Runtime
```
https://dotnet.microsoft.com/download/dotnet/6.0
```
Выберите "ASP.NET Core Runtime 6.0.x" для Windows x64.

### 2. Запустите программу

**Консольный режим:**
```cmd
LZS_inpack.exe
```

**Веб-интерфейс:**
```cmd
LZS_inpack.exe -startweb
```
Затем откройте браузер: `http://localhost:5000`

## Основные возможности

### 🎮 3D Модели
- ✅ Распаковка `.phyre` → `.smd` + `.mesh.ascii`
- ✅ Упаковка `.smd` + `.mesh.ascii` → `.phyre`
- ✅ Поддержка скелетной анимации

### 🔤 Шрифты
- ✅ **Полная экстракция** всех символов (100%)
- ✅ **BMFont формат** (.fnt) для игровых движков
- ✅ **JSON экспорт** с полными метриками
- ✅ **Упаковка обратно** в .phyre формат
- ✅ **Извлечение текстур** (DDS + GTF + PNG)

### 🌐 Веб-интерфейс (НОВОЕ!)
- 📦 **Extract** - Извлечение шрифтов и текстур
- 📦 **Pack** - Упаковка файлов обратно в .phyre
- ✅ **Verify** - Проверка качества упакованных файлов
- 🔄 **Convert** - Конвертация текстур (DDS ↔ PNG ↔ GTF)
- 🔍 **Analyze** - Анализ структуры файлов
- 🎯 **Detect** - Автоматическое определение форматов

## Примеры использования

### Распаковка модели:
```cmd
LZS_inpack.exe model.phyre
```

### Упаковка модели:
```cmd
LZS_inpack.exe -pack model.smd model.mesh.ascii model_new.phyre
```

### Извлечение шрифта:
```cmd
LZS_inpack.exe -extract font.phyre
```

### Упаковка шрифта:
```cmd
LZS_inpack.exe -packfont font.fnt texture.dds font_new.phyre
```

### Запуск веб-интерфейса:
```cmd
LZS_inpack.exe -startweb
```

## Поддерживаемые форматы

- **Phyre Engine** (.phyre) - основной формат
- **SMD** (.smd) - Source Engine формат
- **BMFont** (.fnt) - стандартный формат шрифтов
- **DDS** (.dds) - DirectDraw Surface текстуры
- **GTF** (.gtf) - Sony GTF текстуры
- **PNG** (.png) - для редактирования текстур

## Устранение неполадок

### Веб-интерфейс не запускается:
1. Убедитесь, что .NET 6.0 Runtime установлен
2. Проверьте, что порт 5000 свободен
3. Попробуйте запустить: `LZS_Web.exe --urls "http://localhost:5000"`

### Ошибка "File not found":
- Убедитесь, что все файлы находятся в одной папке
- Проверьте пути к файлам

### Нужна помощь?
- 📖 Полная документация: `README.md`
- 🐛 Сообщить об ошибке: [GitHub Issues](https://github.com/TheLuxifer2023/LZS_inpack/issues)

---

**🎉 Спасибо за использование LZS Phyre Engine Tool v2.7!**
