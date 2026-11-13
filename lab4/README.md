# Лабораторная работа № 4 — ASPA (приложение ASP.NET Core)

**Дисциплина:** Современные технологии серверной разработки
**Тема:** `Map`/API — `MapGet`, `MapPost`, `MapPut`, `MapDelete`, CRUD, Postman, Problem Details
**Цель:** реализовать JSON-DAL с операциями изменения (добавление, удаление, обновление), разработать
minimal API с обработкой ошибок через `UseExceptionHandler`/`Results.Problem`, пройти тесты из условия
в Postman (коллекция прилагается, прогон проверен newman).

> Ответы на вопросы 34–36 — в разделе «Задание 5 (вопросы 34–36) — ответы».

## Состав решения

Решение **ASPA**, .NET 8 (условие допускает 7.0 или 8.0):

| Проект | Тип | Порт | Содержание |
|---|---|---|---|
| `DAL004` | библиотека классов | — | `Celebrity`, `IRepository` (расширенный), `Repository` (JSON + add/del/upd/SaveChanges) |
| `Test_DAL004` | консольное приложение | — | тест новых функций DAL004 (код из условия) |
| `ASPA004_1` | ASP.NET Core (пустой шаблон) | 5050 | GET-список, GET по Id, POST, fallback, обработчик ошибок |
| `ASPA004_2` | то же + DELETE | 5051 | + `MapDelete`, исключение `DeleteByIdException` |
| `ASPA004_3` | то же + PUT | 5052 | + `MapPut` (копия ASPA004_2) |

Порты заданы в `Properties/launchSettings.json`. В условии все запросы идут на `localhost:5050`;
в решении `_2` и `_3` получили 5051/5052, чтобы три приложения можно было держать запущенными
одновременно. При необходимости порт меняется в `launchSettings.json` или флагом `--urls`.

## Запуск

```bash
cd lab4/ASPA
dotnet build                                    # 0 Warning(s), 0 Error(s)

cd Test_DAL004 && dotnet run                     # консольный тест DAL004
cd ../ASPA004_1 && dotnet run                    # http://localhost:5050
cd ../ASPA004_2 && dotnet run                    # http://localhost:5051
cd ../ASPA004_3 && dotnet run                    # http://localhost:5052
```

Запускать нужно **из каталога проекта**: `Repository.Create("Celebrities")` ищет каталог `Celebrities`
в текущем каталоге приложения (в каждом проекте лежит своя копия `Celebrities.json` и фотографий).

Коллекция Postman для всех пунктов условия: `ASPA/postman/ASPA004.postman_collection.json`
(Import → File; переменные `base1` = 5050, `base2` = 5051, `base3` = 5052).

---

## Задание 1 — Postman

Платформа Postman устанавливается с https://www.postman.com/ (обзор — https://habr.com/ru/companies/maxilect/articles/596789/).
Работоспособность проверяется импортом приложенной коллекции и запуском её в Postman
(Run collection). Для каждого шага в коллекции есть assert-скрипты: статус, тело, Id.

Коллекция проверена раннером Postman — newman:

```bash
cd lab4/ASPA
npx --yes newman run postman/ASPA004.postman_collection.json
```

Проверенный прогон: 17 запросов, 16 test-скриптов, 35 assertions, 0 падений.
В GUI Postman импортируйте `postman/ASPA004.postman_collection.json` (Import → File)
и нажмите Run collection — выполняются те же скрипты. Запрос `20-21` выполняется вручную:
он требует удалить `ASPA004_1/Celebrities/Celebrities.json` между запросами.

---

## Задание 2 — DAL004

### Интерфейс `IRepository` (расширен относительно DAL003)

```csharp
public interface IRepository : IDisposable
{
    string BasePath { get; }
    Celebrity[] getAllCelebrities();
    Celebrity? getCelebrityById(int id);
    Celebrity[] getCelebritiesBySurname(string surname);
    string? getPhotoPathById(int id);

    int? addCelebrity(Celebrity celebrity);              // новый: возвращает присвоенный Id
    bool delCelebrityById(int id);                       // новый
    int? updCelebrityById(int id, Celebrity celebrity);   // новый: возвращает новый Id
    int SaveChanges();                                   // новый: запись JSON, число записей
}
```

### Модель

```csharp
public record Celebrity(int Id, string Firstname, string Surname, string PhotoPath);
```

### Реализация `Repository` (ключевые методы)

```csharp
public static string JSONFileName { get; set; } = "Celebrities.json";
public string BasePath { get; }                       // Path.GetFullPath(basePath)
public static IRepository Create(string basePath) => new Repository(basePath);

private int NextId() => _celebrities.Length == 0 ? 1 : _celebrities.Max(c => c.Id) + 1;

public int? addCelebrity(Celebrity celebrity)
{
    int id = NextId();
    _celebrities = _celebrities.Append(celebrity with { Id = id }).ToArray();
    return id;
}

public bool delCelebrityById(int id)
{
    int index = Array.FindIndex(_celebrities, c => c.Id == id);
    if (index < 0) return false;
    _celebrities = _celebrities.Where((_, position) => position != index).ToArray();
    return true;
}

public int? updCelebrityById(int id, Celebrity celebrity)
{
    int index = Array.FindIndex(_celebrities, item => item.Id == id);
    if (index < 0) return null;
    // «удалить + добавить»: новая запись получает очередной свободный Id — как в эталонном выводе теста
    _celebrities = _celebrities.Where((_, position) => position != index).ToArray();
    int newId = NextId();
    _celebrities = _celebrities.Append(celebrity with { Id = newId }).ToArray();
    return newId;
}

public int SaveChanges()
{
    _ = File.ReadAllText(JsonPath);   // контроль доступности источника (FileNotFoundException, если файл удалён)
    File.WriteAllText(JsonPath, JsonSerializer.Serialize(_celebrities, JsonOptions));
    return _celebrities.Length;
}
```

- Изменения копятся **в памяти** (`_celebrities`), на диск попадают только в `SaveChanges()` — как в тесте:
  четыре `addCelebrity` подряд, затем один `SaveChanges()`.
- `NextId()` = максимум по текущему набору + 1. Поэтому после удаления записей 8 и 9 обновление 10-й
  даёт Id 12, а 11-й — Id 13 (это ровно тот вывод, который приведён в условии).
- `SaveChanges()` сначала читает файл: если `Celebrities.json` удалён во время работы приложения,
  возникает `FileNotFoundException` (используется в задании 3, п. 20–21).

### Тест `Test_DAL004` — фактический вывод

Код теста взят из условия; единственное изменение — проверка результата `updCelebrityById` через `!= null`, потому что метод возвращает `int?` (в самом условии интерфейс и код теста противоречат друг другу: `if (int?)` не компилируется). Последовательность вызовов исходная: `addCelebrity` ×4 → `delCelebrityById` ×2 + 1000 → `updCelebrityById` ×2 + 1000.

```text
--- start ----------------
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 2, Firstname = Tim, Surname = Berners-Lee, PhotoPath = /Photo/Berners-Lee.jpg
Id = 3, Firstname = Edgar, Surname = Codd, PhotoPath = /Photo/Codd.jpg
Id = 4, Firstname = Donald, Surname = Knuth, PhotoPath = /Photo/Knuth.jpg
Id = 5, Firstname = Linus, Surname = Torvalds, PhotoPath = /Photo/Torvalds.jpg
Id = 6, Firstname = John, Surname = Neumann, PhotoPath = /Photo/Neumann.jpg
Id = 7, Firstname = Edsger, Surname = Dijkstra, PhotoPath = /Photo/Dijkstra.jpg
--- add 4 ----------------
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 2, Firstname = Tim, Surname = Berners-Lee, PhotoPath = /Photo/Berners-Lee.jpg
Id = 3, Firstname = Edgar, Surname = Codd, PhotoPath = /Photo/Codd.jpg
Id = 4, Firstname = Donald, Surname = Knuth, PhotoPath = /Photo/Knuth.jpg
Id = 5, Firstname = Linus, Surname = Torvalds, PhotoPath = /Photo/Torvalds.jpg
Id = 6, Firstname = John, Surname = Neumann, PhotoPath = /Photo/Neumann.jpg
Id = 7, Firstname = Edsger, Surname = Dijkstra, PhotoPath = /Photo/Dijkstra.jpg
Id = 8, Firstname = TestDel1, Surname = TestDel1, PhotoPath = Photo/TestDel1.jpg
Id = 9, Firstname = TestDel2, Surname = TestDel2, PhotoPath = Photo/TestDel2.jpg
Id = 10, Firstname = TestUpd1, Surname = TestUpd1, PhotoPath = Photo/TestUpd1.jpg
Id = 11, Firstname = TestUpd2, Surname = TestUpd2, PhotoPath = Photo/TestUpd2.jpg
 delete 8
 delete 9
delete 1000 error
--- del 2 ----------------
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 2, Firstname = Tim, Surname = Berners-Lee, PhotoPath = /Photo/Berners-Lee.jpg
Id = 3, Firstname = Edgar, Surname = Codd, PhotoPath = /Photo/Codd.jpg
Id = 4, Firstname = Donald, Surname = Knuth, PhotoPath = /Photo/Knuth.jpg
Id = 5, Firstname = Linus, Surname = Torvalds, PhotoPath = /Photo/Torvalds.jpg
Id = 6, Firstname = John, Surname = Neumann, PhotoPath = /Photo/Neumann.jpg
Id = 7, Firstname = Edsger, Surname = Dijkstra, PhotoPath = /Photo/Dijkstra.jpg
Id = 10, Firstname = TestUpd1, Surname = TestUpd1, PhotoPath = Photo/TestUpd1.jpg
Id = 11, Firstname = TestUpd2, Surname = TestUpd2, PhotoPath = Photo/TestUpd2.jpg
 update 10
 update 11
update 1000 error
--- upd 2 ----------------
Id = 1, Firstname = Noam, Surname = Chomsky, PhotoPath = /Photo/Chomsky.jpg
Id = 2, Firstname = Tim, Surname = Berners-Lee, PhotoPath = /Photo/Berners-Lee.jpg
Id = 3, Firstname = Edgar, Surname = Codd, PhotoPath = /Photo/Codd.jpg
Id = 4, Firstname = Donald, Surname = Knuth, PhotoPath = /Photo/Knuth.jpg
Id = 5, Firstname = Linus, Surname = Torvalds, PhotoPath = /Photo/Torvalds.jpg
Id = 6, Firstname = John, Surname = Neumann, PhotoPath = /Photo/Neumann.jpg
Id = 7, Firstname = Edsger, Surname = Dijkstra, PhotoPath = /Photo/Dijkstra.jpg
Id = 12, Firstname = Updated1, Surname = Updated1, PhotoPath = Photo/Updated1.jpg
Id = 13, Firstname = Updated2, Surname = Updated2, PhotoPath = Photo/Updated2.jpg
```

Вывод совпадает с эталоном из условия: `add 4` → Id 8–11, `delete 1000 error`, `update 1000 error`,
финальные записи Id 12 и 13.

После прогона теста файл `Test_DAL004/Celebrities/Celebrities.json` изменён (в нём уже 9 записей).
Чтобы повторить вывод с теми же Id: `git checkout lab4/ASPA/Test_DAL004/Celebrities/Celebrities.json`.

---

## Задание 3 — ASPA004_1

### Program.cs

```csharp
using DAL004;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
Repository.JSONFileName = "Celebrities.json";

using (IRepository repository = Repository.Create("Celebrities"))
{
    var app = builder.Build();

    app.UseExceptionHandler("/Celebrities/Error");          // 1

    app.MapGet("/Celebrities", () => repository.getAllCelebrities());   // 2

    app.MapGet("/Celebrities/{id:int}", (int id) =>          // 3
    {
        Celebrity? celebrity = repository.getCelebrityById(id);
        if (celebrity == null) throw new FoundByIdException($"Celebrity Id = {id}");
        return celebrity;
    });

    app.MapPost("/Celebrities", (Celebrity celebrity) =>     // 4
    {
        int? id = repository.addCelebrity(celebrity);
        if (id == null) throw new AddCelebrityException("/Celebrities error, id == null");
        if (repository.SaveChanges() <= 0) throw new SaveException("/Celebrities error, SaveChanges() <= 0");
        return new Celebrity((int)id, celebrity.Firstname, celebrity.Surname, celebrity.PhotoPath);
    });

    app.MapFallback((HttpContext ctx) =>                     // 5
        Results.NotFound(new { error = $"Path {ctx.Request.Path} not supported" }));

    app.Map("/Celebrities/Error", (HttpContext ctx) => {     // 6
        Exception? ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        IResult rc = Results.Problem(detail: "Panic", instance: app.Environment.EnvironmentName,
            title: "ASPA004", statusCode: 500);
        if (ex != null)
        {
            if (ex is FoundByIdException) rc = Results.NotFound(ex.Message);
            if (ex is BadHttpRequestException) rc = Results.BadRequest(ex.Message);
            if (ex is SaveException) rc = Results.Problem(title: "ASPA004/SaveChanges", ...);
            if (ex is AddCelebrityException) rc = Results.Problem(title: "ASPA004/AddCelebrity", ...);
            if (ex is FileNotFoundException) rc = Results.Problem(title: "ASPA004", ...);   // добавлено в п.21
        }
        return rc;
    });

    app.Run();
}

public class FoundByIdException : Exception { ... }   // "Found by Id: {message}"
public class SaveException : Exception { ... }        // "SaveChanges error: {message}"
public class AddCelebrityException : Exception { ... }// "AddCelebrityException error: {message}"
```

### Назначение выделенных фрагментов

| № | Фрагмент | Назначение |
|---|---|---|
| 1 | `app.UseExceptionHandler("/Celebrities/Error")` | middleware-перехватчик: необработанное исключение → повторный проход конвейера по `/Celebrities/Error`, ответ 500. Исходное исключение доступно через `IExceptionHandlerFeature` |
| 2 | `app.MapGet("/Celebrities", ...)` | конечная точка GET: возвращает массив `Celebrity[]`, сериализуется в JSON автоматически (200) |
| 3 | `{id:int}` + `throw new FoundByIdException(...)` | констрейнт маршрута: только целое число; если записи нет — пользовательское исключение (не 404 напрямую, статус формирует обработчик) |
| 4 | `MapPost` + `addCelebrity` + `SaveChanges() <= 0` | добавление: Id присваивает DAL; `null` → `AddCelebrityException`; `SaveChanges() <= 0` → `SaveException`; в ответ уходит созданный объект |
| 5 | `app.MapFallback(...)` | «ловушка» для путей, не совпавших ни с одной конечной точкой: 404 + `{"error":"Path <путь> not supported"}`. Именно она отвечает на PUT/DELETE в этой версии приложения и на `/Celebrities/5aa4` (не проходит констрейнт `:int`) |
| 6 | `app.Map("/Celebrities/Error", ...)`, `Results.Problem(...)`, `IResult` | обработчик ошибок: `Results.Problem` формирует Problem Details (RFC 7807/9457) с `title`, `detail`, `status`, `instance`; `IResult` позволяет вернуть разные ответы из одной ветки. `Map` (без указания метода) нужен потому, что конвейер повторно выполняется с **исходным** HTTP-методом (POST/PUT/DELETE), а не только GET |
| 7 | классы исключений | маркерные типы для «своих» ошибок; текст сообщения формируется в конструкторе (`Found by Id: ...`), поэтому в ответ попадает готовый текст |

### Запросы 12–19 (фактические ответы, порт 5050, Development)

| № | Запрос | Код | Тело | Кто сформировал |
|---|---|---|---|---|
| 12.1 | `GET /Celebrities` | 200 | массив 7 объектов | `MapGet` (п.2) |
| 12.2 | `GET /Celebrities/5` | 200 | `{"id":5,...,"surname":"Torvalds",...}` | `MapGet("/Celebrities/{id:int}")` |
| 14 | `GET /Celebrities/5555` | 404 | `"Found by Id: Celebrity Id = 5555"` | `FoundByIdException` → обработчик → `Results.NotFound` |
| 15 | `GET /Celebrities/5aa4` | 404 | `{"error":"Path /Celebrities/5aa4 not supported"}` | `MapFallback` (строка не проходит `{id:int}`) |
| 16 | `POST /Celebrities` (Ada Lovelace) | 200 | `{"id":8,"firstname":"Ada","surname":"Lovelace","photoPath":"/Photo/Lovelace.jpg"}` | `MapPost`, Id — из `addCelebrity`, запись — `SaveChanges` |
| 17 | `GET /Celebrities/8` | 200 | созданная Ada Lovelace | `MapGet` по Id (данные из JSON) |
| 18 | `PUT /Celebrities/8` | 404 | `{"error":"Path /Celebrities/8 not supported"}` | `MapFallback` — PUT ещё не реализован |
| 19 | `DELETE /Celebrities/8` | 404 | `{"error":"Path /Celebrities/8 not supported"}` | `MapFallback` — DELETE ещё не реализован |

Для 18/19 тело содержит путь `/Celebrities/8`, а не `/Celebrities/8 ` с методом: `MapFallback`
видит только `Request.Path`. Статус 404 задан в `Results.NotFound`, тело — анонимный объект `{ error = ... }`.

### Задание 3, п. 20–21 — удаление файла данных и подробность `detail`

**П.20.** Не останавливая `ASPA004_1`, удаляем файл данных и повторяем POST:

```bash
mv ASPA004_1/Celebrities/Celebrities.json ASPA004_1/Celebrities/Celebrities.json.deleted
curl -X POST http://localhost:5050/Celebrities -H 'Content-Type: application/json' \
     -d '{"firstname":"Charlse","surname":"Babbage","photoPath":"/Photo/Babbage.jpg"}'
```

Ответ (500, `application/problem+json`) — «паника» без деталей:

```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"ASPA004","status":500,
 "detail":"Panic","instance":"Development"}
```

**Анализ консоли.** В журнал попадает исходное исключение со стеком:

```text
fail: Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware[1]
      An unhandled exception has occurred while executing the request.
      System.IO.FileNotFoundException: Could not find file '.../ASPA004_1/Celebrities/Celebrities.json'.
      File name: '.../ASPA004_1/Celebrities/Celebrities.json'
         at System.IO.File.ReadAllText(String path, Encoding encoding)
         at DAL004.Repository.SaveChanges() in .../DAL004/Repository.cs:line 101
         at Program.<>c__DisplayClass0_0.<<Main>$>b__2(Celebrity celebrity) in .../ASPA004_1/Program.cs:line 28
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl...
```

Причина: `FileNotFoundException` не входит в набор «своих» исключений, поэтому обработчик оставляет
значение по умолчанию — `detail: "Panic"`. Настоящий текст ошибки доступен только в журнале.

**П.21.** В обработчик добавлена ветка для `FileNotFoundException` — в `detail` уходит `ex.Message`:

```csharp
if (ex is FileNotFoundException)
    rc = Results.Problem(title: "ASPA004", detail: ex.Message,
        instance: app.Environment.EnvironmentName, statusCode: 500);
```

Повторный POST при всё ещё удалённом файле:

```json
{"type":"https://tools.ietf.org/html/rfc9110#section-15.6.1","title":"ASPA004","status":500,
 "detail":"Could not find file '.../ASPA004_1/Celebrities/Celebrities.json'.","instance":"Development"}
```

Отличие от примера в условии только в номере раздела RFC: .NET 8 формирует ссылку на RFC 9110
(в примере — RFC 7231; RFC 9110 заменил 7231 в 2022 г., смысл тот же — 500 Internal Server Error).
Поле `title` совпадает с примером — `ASPA004`.
После опыта файл возвращён на место (`mv ...json.deleted ...json`), приложение продолжает работать.

---

## Задание 3 (второй) — ASPA004_2: DELETE

`Program.cs` — копия `ASPA004_1` + обработка DELETE.

```csharp
// Обработка DELETE-запроса (п.25)
app.MapDelete("/Celebrities/{id:int}", (int id) =>
{
    if (!repository.delCelebrityById(id)) throw new DeleteByIdException($"{CelebrityRoute} error, Id = {id}");
    if (repository.SaveChanges() <= 0) throw new SaveException($"{CelebrityRoute} error, SaveChanges() <= 0");
    return $"Celebrity with Id = {id} deleted";
});

public class DeleteByIdException : Exception
{
    public DeleteByIdException(string message) : base($"Delete by Id:DELETE {message}") { }
}
```

В обработчике ошибок добавлена ветка `if (ex is DeleteByIdException) rc = Results.NotFound(ex.Message);`.
Кроме того, сообщение GET-исключения в этой версии содержит префикс маршрута
(`throw new FoundByIdException($"{CelebrityRoute}, Celebrity Id = {id}")`) — так получается текст
`Found by Id: /Celebrities, Celebrity Id = 9` из условия. В `ASPA004_1` префикса нет
(`Found by Id: Celebrity Id = 5555`), как и показано в п. 14.

### 5 тестов п.27 (фактические ответы, порт 5051)

| № | Запрос | Код | Тело |
|---|---|---|---|
| 1 | `POST /Celebrities` `{"firstname":"ForDelete",...}` | 200 | `{"id":9,"firstname":"ForDelete",...}` (в данных ASPA004_2 уже есть Ada с Id 8) |
| 2 | `GET /Celebrities/9` | 200 | `{"id":9,"firstname":"ForDelete",...}` |
| 3 | `DELETE /Celebrities/9` | 200 | `"Celebrity with Id = 9 deleted"` |
| 4 | `GET /Celebrities/9` (повторно) | 404 | `"Found by Id: /Celebrities, Celebrity Id = 9"` |
| 5 | `DELETE /Celebrities/1000` | 404 | `"Delete by Id:DELETE /Celebrities error, Id = 1000"` |

> В `ASPA004_2/Celebrities/Celebrities.json` уже есть Ada (Id 8) — это состояние после POST
> из сценария ASPA004_1, поэтому первый POST теста получает Id 9, как на скриншоте условия.

---

## Задание 4 — ASPA004_3: PUT

`Program.cs` — копия `ASPA004_2` + обработка PUT.

```csharp
// Обработка PUT-запроса (п.31)
app.MapPut("/Celebrities/{id:int}", (int id, Celebrity celebrity) =>
{
    int? newId = repository.updCelebrityById(id, celebrity);
    if (newId == null) throw new UpdateByIdException($"{CelebrityRoute} error, Id = {id}");
    if (repository.SaveChanges() <= 0) throw new SaveException($"{CelebrityRoute} error, SaveChanges() <= 0");
    return new Celebrity((int)newId, celebrity.Firstname, celebrity.Surname, celebrity.PhotoPath);
});
```

Исключение `UpdateByIdException` → 404 через обработчик (`"Update by Id:PUT /Celebrities error, Id = 1000"`).

### 3 теста п.33 (фактические ответы, порт 5052)

| № | Запрос | Код | Тело |
|---|---|---|---|
| 1 | `POST /Celebrities` `{"firstname":"ForUpdate",...}` | 200 | `{"id":8,"firstname":"ForUpdate",...}` |
| 2 | `PUT /Celebrities/8` `{"firstname":"Updated",...}` | 200 | `{"id":8,"firstname":"Updated","surname":"Updated","photoPath":"/Photo/Updated.jpg"}` |
| 3 | `GET /Celebrities/8` | 200 | то же, что вернул PUT — изменение сохранено |

Особенность DAL004: `updCelebrityById` удаляет старую запись, добавляет новую с очередным Id
и возвращает этот новый Id. Для данных 1–7 + запись 8 это даёт Id 8 (максимум после удаления — 7),
поэтому PUT `/8` и последующий GET `/8` возвращают запись с Id 8, как в условии. Для Id из середины
списка PUT возвращает новый Id (например, PUT `/3` → Id 9), и запись доступна по этому новому Id;
старый Id после обновления не существует — это семантика «удалить + добавить» из эталонного DAL004.

---

## Задание 5 (вопросы 34–36) — ответы

**34. Назначение функций.**

| Функция | Назначение | Где в работе |
|---|---|---|
| `MapGet()` | регистрирует конечную точку для HTTP GET | `/Celebrities`, `/Celebrities/{id:int}` |
| `MapPost()` | для HTTP POST (создание ресурса) | `POST /Celebrities` |
| `MapPut()` | для HTTP PUT (полное обновление ресурса) | `PUT /Celebrities/{id:int}` (ASPA004_3) |
| `MapDelete()` | для HTTP DELETE (удаление ресурса) | `DELETE /Celebrities/{id:int}` (ASPA004_2/3) |
| `MapFallback()` | конечная точка с наименьшим приоритетом: срабатывает, когда ни один маршрут не совпал | ответ `{"error":"... not supported"}` |
| `UseExceptionHandler()` | middleware: перехватывает необработанное исключение, повторно выполняет конвейер по указанному пути, ставит 500 | `/Celebrities/Error` |
| `UseStaticFiles()` | middleware: отдаёт статические файлы (из `wwwroot` или из указанного `FileProvider`) | использовалась в лабе 3 (`/Photo`) |

**35. Назначение объектов и интерфейсов.**

- **`IResult`** — результат выполнения конечной точки: статус + заголовки + тело. Реализации дают
  `Results.Ok()`, `Results.NotFound()`, `Results.Problem()` и т. д.; minimal API сам выполняет `IResult`
  и пишет HTTP-ответ. Позволяет из одной ветки вернуть разные ответы (наш обработчик ошибок).
- **`Exception`** — базовый класс исключений .NET: `Message`, `StackTrace`, `InnerException`.
  От него унаследованы `FoundByIdException`, `SaveException`, `AddCelebrityException`, `DeleteByIdException`,
  `UpdateByIdException`; `FileNotFoundException` и `BadHttpRequestException` — стандартные.
- **`IDisposable`** — контракт детерминированного освобождения ресурсов (`Dispose()`), используется
  конструкцией `using`. `IRepository : IDisposable`, поэтому `using (IRepository repository = ...)` 
  гарантирует вызов `Dispose()` при остановке приложения (в нашем DAL ресурсов нет — метод пустой).
- **`Results.Problem` и RFC 7231, section 6.6.1** — `Results.Problem` формирует ответ Problem Details
  (`application/problem+json`) с полями `type`, `title`, `status`, `detail`, `instance`.
  RFC 7231 §6.6.1 определяет код **500 Internal Server Error** — «сервер столкнулся с неожиданной
  ситуацией». Сам формат Problem Details описан в RFC 7807 (обновлён RFC 9457); в .NET 8 поле `type`
  ссылается уже на RFC 9110 §15.6.1 (замена RFC 7231).
- **`HttpContext`** — контекст одного HTTP-запроса: `Request`, `Response`, `User`, `RequestServices`,
  `Features`, `Items`. В работе используется в `MapFallback` (`ctx.Request.Path`) и в обработчике
  ошибок (`ctx.Features.Get<IExceptionHandlerFeature>()`).

**36. Ada Lovelace и Charles Babbage.**

- **Чарльз Бэббидж (1791–1871)** — английский математик и изобретатель; спроектировал
  разностную машину (Difference Engine) и аналитическую машину (Analytical Engine) — механический
  прообраз универсального компьютера с арифметическим устройством, памятью и перфокарточным вводом.
  Его называют одним из «отцов компьютера».
- **Ада Лавлейс (1815–1852)** — английский математик, дочь Байрона, сотрудничала с Бэббиджем.
  В примечаниях к описанию аналитической машины привела алгоритм вычисления чисел Бернулли —
  первую опубликованную программу, за что считается первым программистом; в её честь назван
  язык Ada. В работе она — персонаж POST-запроса (`Ada Lovelace`), а Бэббидж — персонаж
  запроса из п. 20–21 (`Charlse Babbage`).

---

## Краткая теория: что происходит при запросе

1. **Маршрутизация.** `MapGet`/`MapPost`/`MapPut`/`MapDelete` регистрируют endpoint с шаблоном и
   констрейнтами (`{id:int}`). Порядок методов не важен, важна уникальность пары «метод + шаблон».
2. **Констрейнт не прошёл** → endpoint не выбран → срабатывает `MapFallback` (404, JSON с путём).
3. **Обработчик бросил исключение** → `UseExceptionHandler` перехватывает, ставит 500, сохраняет
   исключение в `IExceptionHandlerFeature` и повторно выполняет конвейер по `/Celebrities/Error`
   с исходным HTTP-методом. Поэтому обработчик зарегистрирован через `app.Map`, а не `MapGet`.
4. **Обработчик ошибок** выбирает `IResult`: NotFound (404) для «не найден», BadRequest (400) для
   ошибки формата, `Results.Problem` (500) для «своих» и файловых ошибок.
5. **Problem Details** — стандартизованное тело ошибки: клиент получает машиночитаемые поля вместо
   HTML-страницы; `instance` = имя среды (`Development`), `title` — короткий код ошибки.

## Проверка (фактические результаты)

| Проверка | Результат |
|---|---|
| `dotnet build ASPA.sln` | 0 Warning(s), 0 Error(s) |
| `Test_DAL004` | вывод совпадает с эталоном условия (Id 8–11, 12–13, `delete/update 1000 error`) |
| ASPA004_1: запросы 12–19 | 200/200/404/404/200/200/404/404 — как в условии |
| ASPA004_1: п.20 (файл удалён) | 500, `title: ASPA004`, `detail: "Panic"`, стек в консоли |
| ASPA004_1: п.21 (ветка FileNotFoundException) | 500, `title: ASPA004`, `detail: "Could not find file '…Celebrities.json'."` |
| ASPA004_2: 5 тестов п.27 | 200 / 200 / 200 / 404 / 404 с текстами из условия |
| ASPA004_3: 3 теста п.33 | 200 / 200 / 200, PUT и GET возвращают `id: 8` с `Updated` |
| ASPA004_3: PUT по Id из середины (`/3`) | 200, в ответе новый Id 9; GET `/9` → 200, GET `/3` → 404 |
| Коллекция Postman (newman) | 17 запросов, 16 test-скриптов, 35 assertions, 0 падений |
| Лаба 3 (регресс) | сборка 0/0; `Test_DAL003` и все endpoint'ы ASPA003 отвечают как в README лабы 3 |

## Источники

- Условие: `labs/Лабораторная_04_ASPNETCORE_Map.pdf`
- [Minimal APIs: MapGet/MapPost/MapPut/MapDelete](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
- [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling) — `UseExceptionHandler`, `IExceptionHandlerFeature`
- [Problem Details / RFC 9457](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling#problem-details)
- [RFC 9110 §15.6.1 — 500 Internal Server Error](https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1)
- [Postman Learning Center](https://learning.postman.com/)
