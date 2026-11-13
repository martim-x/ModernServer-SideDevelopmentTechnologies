# Лабораторная работа № 3 — ASPA (приложение ASP.NET Core)

**Дисциплина:** Современные технологии серверной разработки
**Тема:** паттерн Repository, DAL на JSON, `IDisposable`, статические файлы и обзор каталога
**Цель:** изучить паттерн Repository для доступа к данным в формате JSON, реализовать библиотеку DAL, консольный тест и ASPA с endpoint'ами и настройкой статических файлов/обзора каталога.

> Ответы на вопросы 20–26 находятся в разделе «Ответы на вопросы (20–26)» ниже.

Решение **ASPA** содержит три проекта, .NET 8:

| Проект | Тип | Порт | Содержание |
|---|---|---|---|
| `DAL003` | библиотека классов | — | `IRepository`, `Repository`, `Celebrity` (JSON-DAL) |
| `Test_DAL003` | консольное приложение | — | тестирование `DAL003` |
| `ASPA003` | ASP.NET Core (пустой шаблон) | 5153 | endpoint'ы знаменитостей, статические файлы, обзор каталога |

## Запуск

```bash
cd lab3/ASPA
dotnet build
cd Test_DAL003 && dotnet run             # консольный тест (запуск из каталога проекта)
cd ../ASPA003 && dotnet run              # http://localhost:5153
```

> Запускать `Test_DAL003` нужно именно из каталога проекта: `Repository.Create("Celebrities")`
> разрешает путь от текущего каталога, а не от каталога сборки.

Данные (`Celebrities.json` + фотографии) лежат в каталоге `Celebrities` рядом с каждым приложением,
так как `Repository.Create("Celebrities")` ищет директорий в текущем каталоге приложения.

---

## Задание 1 (DAL003) — библиотека DAL

### Интерфейс `IRepository`

```csharp
public interface IRepository : IDisposable
{
    string BasePath { get; }
    Celebrity[] getAllCelebrities();
    Celebrity? getCelebrityById(int id);
    Celebrity[] getCelebritiesBySurname(string Surname);
    string? getPhotoPathById(int id);
}
```

### Модель `Celebrity`

```csharp
public record Celebrity(int Id, string Firstname, string Surname, string PhotoPath);
```

### Реализация `Repository`

```csharp
public class Repository : IRepository
{
    public static string JSONFileName { get; set; } = "Celebrities.json";
    public string BasePath { get; }
    private Celebrity[] _celebrities = Array.Empty<Celebrity>();

    private Repository(string basePath)
    {
        BasePath = Path.GetFullPath(basePath);
        Load();
    }

    public static IRepository Create(string basePath) => new Repository(basePath);

    private void Load()
    {
        var path = Path.Combine(BasePath, JSONFileName);
        if (!File.Exists(path)) { _celebrities = Array.Empty<Celebrity>(); return; }
        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _celebrities = JsonSerializer.Deserialize<Celebrity[]>(json, options) ?? Array.Empty<Celebrity>();
    }

    public Celebrity[] getAllCelebrities() => _celebrities;
    public Celebrity? getCelebrityById(int id) => _celebrities.FirstOrDefault(c => c.Id == id);
    public Celebrity[] getCelebritiesBySurname(string Surname) =>
        _celebrities.Where(c => c.Surname.Equals(Surname, StringComparison.OrdinalIgnoreCase)).ToArray();
    public string? getPhotoPathById(int id) =>
        _celebrities.FirstOrDefault(c => c.Id == id)?.PhotoPath;
    public void Dispose() { }
}
```

- `JSONFileName` — статическое свойство, задающее имя JSON-файла.
- `BasePath` — полный путь к директорию с JSON и фотографиями (задаётся параметром конструктора).
- Данные десериализуются через `System.Text.Json` с `PropertyNameCaseInsensitive = true` (учитывает разный регистр ключей в JSON).
- `Repository` реализует `IDisposable` (паттерн `using`).

---

## Задание 2 (Test_DAL003) — консольный тест

```csharp
using DAL003;

Repository.JSONFileName = "Celebrities.json";

using (IRepository repository = Repository.Create("Celebrities"))
{
    foreach (Celebrity celebrity in repository.getAllCelebrities()) { ... }
    // getCelebrityById(1), (3), (7), (222)
    // getCelebritiesBySurname("Chomsky"), ("Knuth"), ("XXXX")
    // getPhotoPathById(4), (6), (222)
}
```

### Проверенный результат запуска

```
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 2, Firstname = Tim, Surname = Berners-Lee, PhotoPath = /Photo/Berners-Lee.jpg
Id = 3, Firstname = Edgar, Surname = Codd, PhotoPath = /Photo/Codd.jpg
Id = 4, Firstname = Donald, Surname = Knuth, PhotoPath = /Photo/Knuth.jpg
Id = 5, Firstname = Linus, Surname = Torvalds, PhotoPath = /Photo/Torvalds.jpg
Id = 6, Firstname = John, Surname = Neumann, PhotoPath = /Photo/Neumann.jpg
Id = 7, Firstname = Edsger, Surname = Dijkstra, PhotoPath = /Photo/Dijkstra.jpg
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 3, Firstname = Edgar, Surname = Codd, PhotoPath = /Photo/Codd.jpg
Id = 7, Firstname = Edsger, Surname = Dijkstra, PhotoPath = /Photo/Dijkstra.jpg
Not Found 2222
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 4, Firstname = Donald, Surname = Knuth, PhotoPath = /Photo/Knuth.jpg
PhotoPathById = /Photo/Knuth.jpg
PhotoPathById = /Photo/Neumann.jpg
PhotoPathById =
```

Совпадает с условием лабораторной (включая `Not Found 2222` и пустой результат для Id=222).

---

## Задание 3 (ASPA003) — ASPA

```csharp
var builder = WebApplication.CreateBuilder();
Repository.JSONFileName = "Celebrities.json";
var celebritiesPath = Path.Combine(builder.Environment.ContentRootPath, "Celebrities");

using (IRepository repository = Repository.Create("Celebrities"))
{
    var app = builder.Build();

    app.MapGet("/Celebrities", () => repository.getAllCelebrities());
    app.MapGet("/Celebrities/{id:int}", (int id) => repository.getCelebrityById(id));
    app.MapGet("/Celebrities/BySurname/{surname}", (string surname) =>
        repository.getCelebritiesBySurname(surname));
    app.MapGet("/Celebrities/PhotoPathById/{id:int}", (int id) =>
        repository.getPhotoPathById(id));
    app.MapGet("/", () => "Hello World!");

    // ... UseStaticFiles / UseDirectoryBrowser (см. ниже)

    app.Run();
}
```

### Проверенные результаты (порт 5153, Development)

| Запрос | Код | Тело |
|---|---|---|
| `/` | 200 | `Hello World!` |
| `/Celebrities` | 200 | JSON-массив из 7 знаменитостей |
| `/Celebrities/2` | 200 | `{"id":2,...,"surname":"Berners-Lee",...}` |
| `/Celebrities/BySurname/Chomsky` | 200 | `[{"id":1,...}]` |
| `/Celebrities/PhotoPathById/2` | 200 | `/Photo/Berners-Lee.jpg` |
| `/Photo/Berners-Lee.jpg` | 200 | `image/jpeg`, 176453 байт |

---

## Задание 4 — статические файлы для фотографий

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(celebritiesPath),
    RequestPath = "/Photo"
});
```

Фотографии из каталога `Celebrities` публикуются по адресу `/Photo/<имя>.jpg`,
что позволяет отображать их в браузере (проверено: `/Photo/Berners-Lee.jpg` → 200).
Файл `Linus.jpg` переименован в `Torvalds.jpg`, чтобы совпадать с `PhotoPath` записи Id 5
(проверено: `/Photo/Torvalds.jpg` → 200).

---

## Задание 5 — обзор каталога и скачивание

```csharp
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(celebritiesPath),
    RequestPath = "/Celebrities/download",
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers.Append(
            "Content-Disposition", $"attachment; filename=\"{ctx.File.Name}\"")
});

app.UseDirectoryBrowser(new DirectoryBrowserOptions
{
    FileProvider = new PhysicalFileProvider(celebritiesPath),
    RequestPath = "/Celebrities/download"
});
```

По адресу `/Celebrities/download/` отображается `Index of /Celebrities/download/`
со списком файлов (фото + `Celebrities.json`). `OnPrepareResponse` добавляет заголовок
`Content-Disposition: attachment`, поэтому файл не открывается в браузере, а скачивается
(проверено: `/Celebrities/download/Codd.jpg` → 200, `Content-Disposition: attachment; filename="Codd.jpg"`).

---

## Краткая теория по коду: объекты и как они сделаны (лаба 3)

| Объект | Где | Как сделан | Назначение |
|---|---|---|---|
| `Celebrity` (record) | `DAL003/Celebrity.cs` | `public record Celebrity(int Id, string Firstname, string Surname, string PhotoPath);` | Модель знаменитости (неизменяемая запись) |
| `IRepository` | `DAL003/IRepository.cs` | интерфейс, наследует `IDisposable` | Контракт доступа к данным (подменяемая реализация) |
| `Repository` | `DAL003/Repository.cs` | класс, реализует `IRepository` | JSON-DAL: чтение `Celebrities.json`, поиск по Id/фамилии, путь к фото |
| `Repository.JSONFileName` | `Repository` | `public static string JSONFileName { get; set; }` | Имя JSON-файла (задаётся из приложения) |
| `BasePath` | `Repository` | `Path.GetFullPath(basePath)` в конструкторе | Полный путь к каталогу с JSON и фото |
| `JsonSerializer` | `Repository.Load()` | `JsonSerializer.Deserialize<Celebrity[]>(json, options)` с `PropertyNameCaseInsensitive = true` | Десериализация JSON в массив `Celebrity` |
| `WebApplication` | `ASPA003/Program.cs` | `builder.Build()` | Приложение с endpoint'ами и middleware |
| `StaticFileOptions` | `ASPA003/Program.cs` | объект с `FileProvider`/`RequestPath` | Публикация фото по `/Photo/...` |
| `DirectoryBrowserOptions` | `ASPA003/Program.cs` | объект с `FileProvider`/`RequestPath` | Обзор каталога по `/Celebrities/download/` |
| `PhysicalFileProvider` | `ASPA003/Program.cs` | `new PhysicalFileProvider(celebritiesPath)` | Провайдер файлов для каталога вне `wwwroot` |

---

## Обобщённая теория

### Паттерн Repository

**Repository** — абстракция над источником данных: приложение работает с коллекцией объектов
(`Celebrity`), не зная, откуда они берутся (JSON-файл, БД, память). В `DAL003` источник —
JSON-файл `Celebrities.json` в каталоге `BasePath`.

### Интерфейс `IRepository`

Интерфейс задаёт контракт доступа к данным и позволяет подменять реализацию (например, на БД в лабе 6)
без изменения кода приложения. В `ASPA003` и `Test_DAL003` используется именно интерфейс `IRepository`,
а не конкретный `Repository`.

### `IDisposable` и конструкция `using`

`IRepository : IDisposable`. Конструкция `using (IRepository repository = Repository.Create(...)) { ... }`
гарантирует вызов `Dispose()` при выходе из блока (в т.ч. при исключении), освобождая ресурсы
(файловые дескрипторы, соединения). В `ASPA003` блок `using` оборачивает `app.Run()`,
поэтому репозиторий живёт всё время работы приложения и освобождается при остановке.

### Сериализация и десериализация

- **Сериализация** — преобразование объекта в строку/байты (например, объект `Celebrity` → JSON).
- **Десериализация** — обратное преобразование (JSON-строка → объекты `Celebrity[]`).
В `Repository.Load()` используется `JsonSerializer.Deserialize<Celebrity[]>(json, options)`.

### JSON

**JSON (JavaScript Object Notation)** — текстовый формат обмена данными, массив объектов
`[{ "Id": 1, "Firstname": "Noam", ... }]`. В `DAL003` файл `Celebrities.json`
хранит список знаменитостей; `PhotoPath` содержит относительный URL фотографии (`/Photo/...jpg`).

---

## Ответы на вопросы (20–26)

**20. Назначение паттерна Repository.**
Абстрагировать доступ к данным: приложение работает с коллекцией объектов, не зная источника
(JSON-файл, БД). Упрощает тестирование и замену источника данных.

**21. Для чего в паттерне Repository применяется интерфейс `IRepository`?**
Задаёт контракт доступа к данным и позволяет подменять реализацию (например, JSON → БД)
без изменения кода, зависящего от интерфейса. Поддерживает инверсию зависимостей.

**22. Для чего применяется интерфейс `IDisposable`?**
Для освобождения неуправляемых ресурсов (файлы, соединения) через `Dispose()`.
Конструкция `using` гарантирует вызов `Dispose()` даже при исключении.

**23. Поясните работу конструкции `using (IRepository repository = Repository.Create(...)) { ... }`.**
Создаёт объект, выполняет блок, затем вызывает `repository.Dispose()` (даже при исключении).
В `ASPA003` блок оборачивает `app.Run()`, поэтому репозиторий живёт всё время работы сервера.

**24. Смысл слов: сериализация и десериализация.**
Сериализация — объект → JSON-строка/байты. Десериализация — JSON-строка → объекты.
В `Repository.Load()` JSON десериализуется в `Celebrity[]`.

**25. Расшифруйте аббревиатуру JSON, поясните смысл.**
JavaScript Object Notation — текстовый формат обмена данными в виде пар «ключ: значение»
и массивов объектов; легко читается человеком и машиной.

**26. Кто такой Эдсгер Дейкстра?**
Эдсгер Вибе Дейкстра (1930–2002) — нидерландский учёный-информатик, лауреат премии Тьюринга (1972).
Известен алгоритмом Дейкстры (кратчайший путь), семафорами, структурным программированием,
статьёй «Go To Statement Considered Harmful». Один из знаменитостей в наборе данных лабы.

---

## Проверка сборки

```bash
cd lab3/ASPA
dotnet build   # 0 Warning(s), 0 Error(s)
```

---

## Источники

- Документация Microsoft Learn: [Static files in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/static-files)
- [Directory browsing](https://learn.microsoft.com/aspnet/core/fundamentals/static-files#directory-browsing)
- [System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json-overview)
- Условие: `labs/Лабораторная_03_ASPNETCORE_1.pdf`
