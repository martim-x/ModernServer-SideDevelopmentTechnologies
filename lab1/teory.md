# Лабораторная 1 — ASP.NET Core (решение ASPA, проект ASPA001)

Теория и рабочие инструкции к заданиям 1–5. Все команды и выводы в этом файле
проверены на этой машине: проект создан, собран, запущен, HTTP Logging
протестирован.

- [0. Окружение и версии](#0-окружение-и-версии)
- [1. Создание решения и проекта](#1-создание-решения-и-проекта)
- [2. Подключение зависимостей](#2-подключение-зависимостей)
- [3. Состав проекта: назначение файлов](#3-состав-проекта-назначение-файлов)
- [4. Debug и Release](#4-debug-и-release)
- [5. Запуск без Visual Studio](#5-запуск-без-visual-studio)
- [6. Program.cs в стиле Program.Main](#6-programcs-в-стиле-programmain)
- [7. HTTP Logging](#7-http-logging-задание-4)
- [8. Теория](#8-теория-задание-5)

## 0. Окружение и версии

Задание 1 — определить версию .NET:

```bash
dotnet --version         # версия активного SDK
dotnet --list-sdks       # все установленные SDK
dotnet --list-runtimes   # все установленные рантаймы
dotnet --info            # всё сразу: SDK, рантаймы, RID, пути
```

Результат на этой машине:

| Что | Значение |
|---|---|
| SDK | 10.0.300 (единственный) |
| Microsoft.NETCore.App | 8.0.20, 10.0.8 |
| Microsoft.AspNetCore.App | 8.0.20, 10.0.8 |
| ОС | macOS (darwin 27.0) |

Лаба требует `net7.0` или `net8.0`, а SDK установлен только 10-й — это рабочая
ситуация. SDK старшей версии умеет собирать проекты под младшие целевые
платформы: пакеты таргетинга (`Microsoft.AspNetCore.App.Ref` 8.x) автоматически
скачиваются из NuGet, а запуск обеспечивает установленный рантайм 8.0.20.
Шаблона `net7.0` в SDK 10 уже нет (доступны только `net8.0` и `net10.0`), к тому
же .NET 7 снят с поддержки в мае 2024 — поэтому берём **net8.0**.

## 1. Создание решения и проекта

### 1.1 Через dotnet CLI

```bash
cd lab1
mkdir ASPA && cd ASPA

# решение с именем ASPA
dotnet new sln -n ASPA --format sln

# пустой веб-проект ASPA001 под .NET 8 в подкаталоге ASPA001
dotnet new web -n ASPA001 -o ASPA001 -f net8.0

# привязать проект к решению
dotnet sln ASPA.sln add ASPA001/ASPA001.csproj

# собрать и запустить
dotnet build
dotnet run --project ASPA001
```

Получается структура, которую ждёт лаба:

```
lab1/ASPA/
├── ASPA.sln
└── ASPA001/
    ├── ASPA001.csproj
    ├── Program.cs
    ├── appsettings.json
    ├── appsettings.Development.json
    └── Properties/launchSettings.json
```

Флаг `--format sln` важен: SDK 10 по умолчанию создаёт решение в новом формате
`ASPA.slnx`, который не открывается старыми версиями Visual Studio. С флагом
получается классический `ASPA.sln`.

Проверка работоспособности (задание 2) — в браузере или из терминала:

```bash
curl -i http://localhost:5255/
# HTTP/1.1 200 OK
# Content-Type: text/plain; charset=utf-8
# Server: Kestrel
#
# Hello World!
```

### 1.2 Через Visual Studio / Rider

File → New → Project → **ASP.NET Core Empty** → Solution name `ASPA`,
Project name `ASPA001`, Framework `.NET 8.0`. Галочку «Place solution and
project in the same directory» нужно снять, иначе решение и проект окажутся в
одном каталоге.

### 1.3 Полезные ключи шаблона web

| Ключ | Что делает |
|---|---|
| `-f net8.0` | целевая платформа (`net8.0` или `net10.0`) |
| `-n ИМЯ` | имя проекта и сборки |
| `-o КАТАЛОГ` | куда положить файлы |
| `--use-program-main` | явный `class Program` с `Main` вместо top-level statements (задание 3) |
| `--no-https` | без HTTPS-профиля и редиректа на https |
| `--exclude-launch-settings` | без `Properties/launchSettings.json` |
| `--no-restore` | не восстанавливать пакеты сразу |
| `--dry-run` | показать, что будет создано, ничего не создавая |

Посмотреть все шаблоны и все ключи конкретного шаблона:

```bash
dotnet new list          # список установленных шаблонов
dotnet new web --help    # все параметры шаблона web
```

## 2. Подключение зависимостей

### 2.1 NuGet-пакеты

```bash
cd lab1/ASPA/ASPA001

dotnet add package Swashbuckle.AspNetCore                     # последняя версия
dotnet add package Swashbuckle.AspNetCore --version 6.6.2     # конкретная версия
dotnet add package Serilog.AspNetCore --prerelease            # включая предрелизы

dotnet list package               # что подключено и какие версии разрешились
dotnet list package --outdated    # для чего есть обновления
dotnet list package --vulnerable  # известные уязвимости
dotnet remove package Swashbuckle.AspNetCore
dotnet restore                    # восстановить пакеты по .csproj
```

Версию указывайте явно. Без `--version` ставится самая свежая версия пакета:
при проверке `dotnet add package Swashbuckle.AspNetCore` в проект под `net8.0`
подтянулась версия 10.2.3, рассчитанная на .NET 10. Собираться будет, но это
источник неожиданных поломок. Правило: мажорная версия основного пакета должна
совпадать с мажорной версией целевой платформы.

### 2.2 Ссылка на другой проект

```bash
dotnet add ASPA001/ASPA001.csproj reference ASPA002/ASPA002.csproj
dotnet list reference
dotnet remove reference ../ASPA002/ASPA002.csproj
```

### 2.3 Прямая правка .csproj

Команды выше просто дописывают элементы в `.csproj` — то же можно сделать руками:

```xml
<ItemGroup>
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  <ProjectReference Include="..\ASPA002\ASPA002.csproj" />
</ItemGroup>
```

После правки — `dotnet restore`.

### 2.4 Что подключать НЕ нужно

ASP.NET Core поставляется как *shared framework* — метапакет
`Microsoft.AspNetCore.App`, который подключается автоматически строкой
`Sdk="Microsoft.NET.Sdk.Web"` в `.csproj`. Поэтому Kestrel, внедрение
зависимостей, логирование, маршрутизация, конфигурация JSON, `HttpLogging`,
`HostFiltering` уже доступны и отдельных `PackageReference` не требуют.
Проверить, что нужный компонент входит в framework:

```bash
ls /usr/local/share/dotnet/shared/Microsoft.AspNetCore.App/8.0.20/ | grep HttpLogging
# Microsoft.AspNetCore.HttpLogging.dll
```

Где физически лежат скачанные пакеты — `~/.nuget/packages`.
Полный граф зависимостей после restore — `obj/project.assets.json`.

## 3. Состав проекта: назначение файлов

### ASPA001.csproj

В задании он назван `.cproj` — это опечатка, расширение файла проекта C# —
`.csproj`. Это файл сборки в формате MSBuild (XML):

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

- `Sdk="Microsoft.NET.Sdk.Web"` — набор правил сборки для веб-приложений:
  ссылка на shared framework ASP.NET Core, автоматическое включение всех `.cs`
  файлов каталога, копирование `appsettings*.json` в вывод, поддержка `wwwroot`;
- `TargetFramework` — целевая платформа, определяет доступный API и рантайм;
- `Nullable` — анализ ссылочных типов на `null` во время компиляции;
- `ImplicitUsings` — неявные глобальные `using` (`System`, `System.Linq`,
  `Microsoft.AspNetCore.Builder` и др.), поэтому в `Program.cs` их не видно;
- сюда же добавляются `PackageReference` и `ProjectReference`.

Назначение файла: он полностью описывает, что и как собирать — целевую
платформу, зависимости, параметры компиляции. Заменяет собой и описание
проекта, и список файлов, и настройки сборки.

### Program.cs

Точка входа приложения. Отвечает за четыре вещи в строгом порядке: создание
построителя хоста → регистрация сервисов в контейнере внедрения зависимостей →
построение конвейера middleware и маршрутов → запуск сервера. Разбор кода — в
разделе 6.

### appsettings.json

Конфигурация приложения, читается при запуске: уровни логирования, строки
подключения, собственные секции. Меняется без перекомпиляции, копируется в
`bin` и разворачивается вместе с приложением.

`appsettings.Development.json` перекрывает базовый файл, когда переменная
`ASPNETCORE_ENVIRONMENT` равна `Development`. Полный порядок источников
конфигурации (каждый следующий перекрывает предыдущий):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (только в Development)
4. переменные окружения
5. аргументы командной строки

Содержимое по умолчанию:

```json
{
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  },
  "AllowedHosts": "*"
}
```

`AllowedHosts` — фильтр допустимых значений заголовка `Host` (защита от host
header injection), `*` означает «разрешены любые».

### Properties/launchSettings.json

Настройки **только для локального запуска** из IDE и `dotnet run`. В
опубликованное приложение не попадают, на сервере не используются. Хранит
профили запуска: переменные окружения, открывать ли браузер, адреса
прослушивания.

**TCP-порт задаётся здесь** — в `profiles → <профиль> → applicationUrl`:

```json
"profiles": {
  "http":  { "applicationUrl": "http://localhost:5255" },
  "https": { "applicationUrl": "https://localhost:7141;http://localhost:5255" }
},
"iisSettings": {
  "iisExpress": { "applicationUrl": "http://localhost:52725", "sslPort": 44370 }
}
```

Для IIS Express порт задаётся отдельно, в секции `iisSettings`. Переопределить
порт при запуске, не трогая файл:

```bash
dotnet run --urls http://localhost:5080
ASPNETCORE_URLS=http://localhost:5080 dotnet ASPA001.dll
```

Выбрать профиль запуска: `dotnet run --launch-profile https`.

### bin/ и obj/

`obj/` — промежуточные результаты сборки: граф зависимостей
`project.assets.json`, сгенерированный код, объектные файлы.
`bin/<Конфигурация>/net8.0/` — готовая сборка. Оба каталога генерируются
автоматически и в git не коммитятся.

## 4. Debug и Release

```bash
dotnet build -c Release      # пересборка в Release
dotnet run -c Release
dotnet clean                 # очистить вывод перед честной пересборкой
```

| | Debug | Release |
|---|---|---|
| Оптимизация IL (`Optimize`) | выключена | включена |
| Константы компиляции | `DEBUG`, `TRACE` | только `TRACE` |
| Инлайнинг методов, свёртка кода | нет | да |
| `Debug.Assert`, `Debug.WriteLine` | работают | вырезаются компилятором |
| Скорость выполнения | ниже | выше |
| Отладка по шагам | точное соответствие строкам кода | строки «съезжают» из-за оптимизаций |
| Каталог вывода | `bin/Debug/net8.0/` | `bin/Release/net8.0/` |
| Назначение | разработка и отладка | поставка, продакшн |

Файл `.pdb` с отладочными символами в современном SDK создаётся в обеих
конфигурациях — разница не в его наличии, а в оптимизации и константах
компиляции. Именно из-за оптимизаций отладчик в Release ведёт себя
непредсказуемо: локальные переменные «оптимизированы», точки останова
смещаются, стектрейсы короче из-за инлайнинга.

## 5. Запуск без Visual Studio

```bash
dotnet build -c Release
cd ASPA001/bin/Release/net8.0

dotnet ASPA001.dll   # запуск через утилиту dotnet (кроссплатформенно)
./ASPA001            # запуск через apphost; на Windows — ASPA001.exe
```

Про `ASPA001.exe`: на macOS и Linux файла с расширением `.exe` нет — apphost
называется просто `ASPA001` и является нативным исполняемым файлом. `.exe`
появляется только при сборке на Windows или при публикации под Windows
(`dotnet publish -r win-x64`).

Минимально необходимый набор файлов для переноса в отдельный каталог
(framework-dependent приложение):

| Файл | Зачем |
|---|---|
| `ASPA001.dll` | сам код в IL — главный файл |
| `ASPA001.runtimeconfig.json` | какой рантайм и какой версии грузить; без него хост не стартует |
| `ASPA001.deps.json` | список зависимостей для загрузчика сборок |
| `appsettings.json` | конфигурация, если приложение её читает |
| `ASPA001` / `ASPA001.exe` | нужен только для запуска без `dotnet` |

`ASPA001.pdb` копировать не нужно — он влияет только на подробность
стектрейсов при отладке.

Правильный способ получить такой набор — не копировать файлы из `bin` руками,
а опубликовать:

```bash
dotnet publish -c Release -o ./out                      # нужен установленный рантайм
dotnet publish -c Release -r osx-arm64 --self-contained # вместе с рантаймом, ~70 МБ
dotnet publish -c Release -r win-x64 --self-contained   # получится out/ASPA001.exe
```

Self-contained вариант запускается на машине, где .NET вообще не установлен —
рантайм лежит рядом с приложением.

## 6. Program.cs в стиле Program.Main

Шаблон генерирует top-level statements — код без класса и метода:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
```

Это синтаксический сахар: компилятор сам оборачивает такой файл в класс
`Program` с методом `Main`, а `args` — параметр этого метода. Явная запись
(задание 3), с комментарием к каждой строке:

```csharp
// Пространство имён нужно для типа HttpLoggingFields (задание 4).
using Microsoft.AspNetCore.HttpLogging;

// Класс-контейнер точки входа; internal — виден только внутри своей сборки.
internal class Program
{
    // Метод, с которого рантайм начинает выполнение; args — аргументы командной строки.
    private static void Main(string[] args)
    {
        // Построитель приложения: читает конфигурацию (appsettings, переменные
        // окружения, args), настраивает логирование, DI-контейнер и Kestrel.
        var builder = WebApplication.CreateBuilder(args);

        // Регистрация сервисов в контейнере внедрения зависимостей.
        // Всё, что добавляется в builder.Services, должно быть ДО вызова Build().
        builder.Services.AddHttpLogging(o => o.LoggingFields = HttpLoggingFields.All);

        // Фиксируем конфигурацию и контейнер, получаем готовый объект приложения.
        var app = builder.Build();

        // Конвейер middleware: порядок вызовов Use* задаёт порядок обработки запроса.
        app.UseHttpLogging();

        // Маршрут: GET по адресу "/" возвращает строку в теле ответа.
        app.MapGet("/", () => "Hello World!");

        // Kestrel открывает TCP-порт и блокирует поток до остановки приложения.
        app.Run();
    }
}
```

Сгенерировать такой скелет сразу: `dotnet new web --use-program-main`.
В Visual Studio ту же правку делает рефакторинг «Convert to 'Program.Main'
style program» (Ctrl+. на первой строке файла).

## 7. HTTP Logging (задание 4)

NuGet-пакет ставить не нужно: `Microsoft.AspNetCore.HttpLogging.dll` входит в
shared framework (проверено в `Microsoft.AspNetCore.App/8.0.20/`).

Шаг 1 — сервис и middleware в `Program.cs`:

```csharp
using Microsoft.AspNetCore.HttpLogging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpLogging(o =>
{
    o.LoggingFields = HttpLoggingFields.All; // метод, путь, заголовки, тела, код, длительность
    o.RequestBodyLogLimit = 4096;            // сколько байт тела запроса писать в лог
    o.ResponseBodyLogLimit = 4096;
});

var app = builder.Build();

app.UseHttpLogging();   // как можно раньше в конвейере, иначе часть запросов не попадёт в лог

app.MapGet("/", () => "Hello World!");

app.Run();
```

Шаг 2 — **обязательно** поднять уровень логирования, иначе задание будет
выглядеть неработающим. В шаблонном `appsettings.Development.json` стоит
`"Microsoft.AspNetCore": "Warning"`, а HttpLogging пишет на уровне
`Information` — по умолчанию в консоли не появится ни одной строки. Проверено
экспериментально: с настройками по умолчанию — 0 записей, после правки — 4
записи на один запрос.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware": "Information"
    }
  }
}
```

Шаг 3 — проверка:

```bash
dotnet run --project ASPA001
curl -s http://localhost:5255/
```

Ожидаемый вывод в консоли приложения:

```
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[1]
      Request:
      Protocol: HTTP/1.1
      Method: GET
      Scheme: http
      Path: /
      Host: localhost:5255
      User-Agent: curl/8.10.1
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[2]
      Response:
      StatusCode: 200
      Content-Type: text/plain; charset=utf-8
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[4]
      ResponseBody: Hello World!
info: Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware[8]
      Duration: 3.8569ms
```

Вместо `HttpLoggingFields.All` флаги можно набирать точечно, например
`HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath |
HttpLoggingFields.ResponseStatusCode`. `All` включает тела запросов и ответов —
в реальном приложении так делать не стоит: в логи попадут пароли и токены.

## 8. Теория (задание 5)

### Что такое фреймворк?

Каркас приложения: набор библиотек плюс заданная архитектура и точки
расширения. Ключевое отличие от библиотеки — инверсия управления: библиотеку
вызываете вы, а фреймворк вызывает ваш код в нужный момент. Фреймворк диктует
структуру программы, а вы дописываете недостающие части: обработчики,
контроллеры, конфигурацию.

### Что такое фреймворк операционной системы?

Набор системных библиотек и API, поставляемых вместе с ОС и жёстко к ней
привязанных: Win32/WinAPI и .NET Framework в Windows, Cocoa, Foundation, AppKit
в macOS (там это буквально каталоги `*.framework`). Такой фреймворк даёт доступ
к службам конкретной ОС — окнам, файловой системе, сети, реестру — и приложение
на его основе работает только на этой ОС. Противоположность —
кроссплатформенный фреймворк вроде ASP.NET Core, который прячет системные
вызовы за общими абстракциями.

### Что такое программная платформа?

Совокупность среды исполнения, библиотек, компиляторов и инструментов, на
которой создаются и работают программы. Платформа отвечает на вопрос «на чём
это выполняется», фреймворк — «по каким правилам это написано». .NET —
платформа: рантайм CoreCLR, библиотека классов BCL и SDK. ASP.NET Core —
фреймворк, работающий на этой платформе.

### Что такое .NET?

Кроссплатформенная платформа разработки Microsoft с открытым исходным кодом
(лицензия MIT). Состав:

- **CoreCLR** — среда исполнения: JIT-компилятор, сборщик мусора, загрузчик сборок;
- **BCL** — библиотека базовых классов (пространства `System.*`);
- **компиляторы** — Roslyn для C# и VB, компилятор F#;
- **SDK** — утилита `dotnet`, MSBuild, шаблоны проектов, клиент NuGet.

Компиляция двухступенчатая: исходный код → промежуточный язык IL (сборка
`.dll`) → машинный код при первом вызове метода (JIT) либо заранее, при
публикации (AOT). Начиная с .NET 5 это единая платформа, заменившая .NET
Framework, .NET Core и Mono; новые мажорные версии выходят каждый ноябрь.

### Что такое ASP.NET CORE?

Веб-фреймворк платформы .NET для серверных приложений: сайтов, REST API,
приложений реального времени (SignalR), Blazor. Построен на двух идеях —
внедрение зависимостей и конвейер middleware, через который последовательно
проходит каждый запрос. Включает собственный веб-сервер Kestrel, подсистемы
конфигурации, логирования, маршрутизации, аутентификации и авторизации. Модели
программирования: minimal API (как в этой лабе), MVC, Razor Pages.

### Что означает, что ASP.NET CORE является кроссплатформенной?

Один и тот же исходный код и одни и те же собранные IL-сборки работают на
Windows, Linux и macOS, на архитектурах x64 и ARM64. Обеспечивается это тем, что
под каждую ОС существует своя реализация рантайма, а веб-сервер Kestrel не
зависит от IIS. Практические следствия: разработка на macOS, продакшн на Linux
в Docker; публикация под конкретную ОС ключом `-r linux-x64`. Ограничение:
платформозависимые вещи (реестр Windows, интеграция с IIS, часть
`System.Drawing`) в кроссплатформенном коде недоступны.

### Что такое NuGet?

Менеджер пакетов .NET. Пакет — файл `.nupkg` (zip со сборками, метаданными и
списком зависимостей). Публичный репозиторий — nuget.org, клиент встроен в
утилиту `dotnet`. Зависимость объявляется в `.csproj` элементом
`PackageReference`, восстанавливается командой `dotnet restore` в локальный кеш
`~/.nuget/packages`, а разрешённый граф версий записывается в
`obj/project.assets.json`. Сама ASP.NET Core тоже распространяется через NuGet —
в виде shared framework.

### Для чего нужна утилита dotnet?

Единый интерфейс командной строки к SDK и одновременно хост рантайма:

| Команда | Назначение |
|---|---|
| `dotnet new` | создание проектов и решений из шаблонов |
| `dotnet restore` | восстановление NuGet-зависимостей |
| `dotnet build` | компиляция в IL |
| `dotnet run` | сборка и запуск проекта |
| `dotnet ASPA001.dll` | запуск уже собранного приложения (режим хоста) |
| `dotnet test` | запуск тестов |
| `dotnet publish` | подготовка к развёртыванию |
| `dotnet add/list/remove package` | управление зависимостями |
| `dotnet sln` | управление составом решения |
| `dotnet tool` | установка инструментов, например `dotnet-ef` |
| `dotnet --info` | диагностика окружения |

Позволяет пройти весь цикл разработки без IDE — это и есть кроссплатформенный
способ работы с .NET.

### Что такое CoreCLR?

Среда исполнения (виртуальная машина) .NET, кроссплатформенная реализация CLR.
Загружает сборки, компилирует IL в машинный код компилятором RyuJIT, управляет
памятью через сборщик мусора, обрабатывает исключения, потоки и вызовы нативного
кода. Ядро .NET Core и .NET 5+. Альтернативный режим — Native AOT, когда
машинный код создаётся при публикации и JIT в работе приложения не участвует.

### Что такое CoreFX?

Историческое название реализации библиотеки классов для .NET Core — коллекции,
файловый ввод-вывод, LINQ, сеть, сериализация, то есть пространства `System.*`.
Репозиторий `dotnet/corefx` в 2019 году был объединён с `coreclr` в общий
`dotnet/runtime`, поэтому термин сегодня встречается в основном в старой
документации. Пара «CoreCLR + CoreFX» = «рантайм + библиотека классов».

### Как называется встроенный в ASP.NET CORE http-сервер?

**Kestrel** — кроссплатформенный асинхронный веб-сервер, работающий внутри
процесса приложения. Виден в заголовке ответа `Server: Kestrel`. Может работать
как пограничный сервер, обращённый в интернет, или за реверс-прокси (Nginx, IIS,
Apache) — тогда прокси берёт на себя TLS, статику и балансировку.
Альтернативные серверы: HTTP.sys (только Windows) и IIS Express для отладки.

### Стадии (этапы) подготовки и запуска ASPA

1. **restore** — разбор `.csproj`, разрешение графа NuGet-зависимостей, запись
   `obj/project.assets.json`;
2. **build** — Roslyn компилирует C# в IL; в `bin/<Конфигурация>/net8.0/`
   появляются `ASPA001.dll`, `ASPA001.deps.json`,
   `ASPA001.runtimeconfig.json`, apphost и копии `appsettings*.json`;
3. **publish** — для развёртывания собирается самодостаточный набор файлов;
4. **старт процесса** — apphost или `dotnet ASPA001.dll` читает
   `runtimeconfig.json`, находит рантайм нужной версии, инициализирует CoreCLR и
   вызывает `Main`;
5. **построение хоста** — `WebApplication.CreateBuilder(args)`: чтение
   конфигурации по цепочке источников, настройка логирования, DI, Kestrel;
6. **регистрация сервисов** в DI-контейнере (`builder.Services...`);
7. **`builder.Build()`** — контейнер «запечатывается», создаётся объект
   `WebApplication`;
8. **построение конвейера** middleware и таблицы маршрутов (`app.Use*`,
   `app.MapGet`);
9. **`app.Run()`** — Kestrel открывает TCP-порт и начинает слушать соединения;
10. **обработка запроса** — сокет → Kestrel → конвейер middleware → endpoint →
    формирование ответа;
11. **остановка** — по сигналу (Ctrl+C, SIGTERM) активные запросы
    доигрываются, ресурсы освобождаются.

### Последняя устойчивая версия ASP.NET CORE

**.NET 10 / ASP.NET Core 10.0** — LTS, вышла в ноябре 2025; на этой машине
установлен рантайм 10.0.8. Мажорные версии выпускаются раз в год в ноябре:
чётные — LTS с длинным сроком поддержки, нечётные — STS с коротким.

Про версию из задания: .NET 8 — тоже LTS, но её поддержка заканчивается
10 ноября 2026 года, а .NET 7 снят с поддержки ещё в мае 2024.

Актуальное состояние всегда здесь:
<https://dotnet.microsoft.com/download/dotnet> и
<https://learn.microsoft.com/dotnet/core/releases-and-support>















