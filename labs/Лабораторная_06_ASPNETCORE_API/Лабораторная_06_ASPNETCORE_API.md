# Лабораторная 6. ASPA — API (Celebrities: EF Core, Scoped, MapGroup, middleware-ошибки)

## Назначение
Перевести DAL на Entity Framework с MS SQL Server, реализовать репозиторий как **Scoped-сервис**
с конфигурацией через `IOptions<CelebritiesConfig>`, построить полный REST API знаменитостей
и событий через `MapGroup` и настроить middleware-обработчик ошибок; затем отобразить данные
на html-странице Index.html.

## Обозначения в ТЗ
> **♣ — задания, требующие самостоятельного изучения (нет в лекциях)**

♣ отмечен: Задание 1 (схема БД) и Задание 3 (консольный тест) — целиком помечены в тексте PDF.

## Требуемые проекты

| Проект | Тип | Назначение |
|---|---|---|
| **DAL_Celebrity** | Библиотека классов | интерфейсы `IRepository<T1,T2>`, `IMix`, `ICelebrity<T>`, `ILifeevent<T>` |
| **DAL_Celebrity_MSSQL** | Библиотека классов | реализация `IRepository` на Entity Framework + класс `Init` |
| **DAL_Celebrity_MSSQL_Test** | Консольное приложение | тестирование репозитория |
| **ASPA006_1** | ASP.NET Core (пустой шаблон) | API знаменитостей и событий |

Имя решения: **ASPA**. Версия .NET: **7.0 или 8.0**.

## Задание 1 (♣) — схема базы данных LES
1. Исследуйте логическую схему базы данных **«Life Events of Celebrities» (LES)** и поясняющие
   назначение полей классы, соответствующие (ORM) таблицам LES.

### Схема
- Таблица **Celebrities**: Id (ключ), FullName, Nationality, ReqPhotoPath
- Таблица **Lifeevents**: Id (ключ), CelebrityId (-> Celebrities.Id, FK), Date, Description, ReqPhotoPath

```csharp
public class Celebrity    // Знаменитость
{
    public Celebrity() { this.FullName = string.Empty; this.Nationality = string.Empty; }
    public int Id { get; set; }                        // Id Знаменитости
    public string FullName { get; set; }               // полное имя Знаменитости
    public string Nationality { get; set; }            // гражданство Знаменитости (2 символа ISO)
    public string? ReqPhotoPath { get; set; }           // reguest path Фотографии
    public virtual bool Update(Celebrity celebrity)   // вспомогательный метод
}

public class Lifeevent    // Событие в жизни знаменитости
{
    public Lifeevent() { this.Description = string.Empty; }
    public int Id { get; set; }                        // Id События
    public int CelebrityId { get; set; }               // Id Знаменитости
    public DateTime? Date { get; set; }                // дата События
    public string Description { get; set; }            // описание События
    public string? ReqPhotoPath { get; set; }           // reguest path Фотографии
    public virtual bool Update(Lifeevent lifeevent)    // вспомогательный метод
}
```

## Задание 2 — библиотеки DAL
2. Разработайте библиотеку **DAL_Celebrity**, содержащую следующие интерфейсы
   (прилагается файл **DAL_Celebrity.cs**).
3. Разработайте библиотеку **DAL_Celebrity_MSSQL**, реализующую интерфейс `IRepository`
   (DAL_Celebrity) с применением **Entity Framework** (NuGet-пакет).
4. Добавьте в библиотеку DAL_Celebrity_MSSQL класс **Init**, предназначенный для инициализации
   и тестирования БД (файл **Init.cs** прилагается).

### Интерфейсы DAL_Celebrity (задание 2) — полностью читаемы со скриншота
```csharp
public interface IRepository<T1, T2> :  IMix<T1, T2>, ICelebrity<T1>, ILifeevent<T2> { }

public interface IMix<T1, T2>
{
    List<T2> GetLifeeventsByCelebrityId(int celebrityId);   // получить все События по Id Знаменитости
    T1? GetCelebrityByLifeeventId(int lifeeventId);         // получить Знаменитость по Id События
}
public interface ICelebrity<T> : IDisposable
{
    List<T> GetAllCelebrities();                      // получить все Знаменитости
    T? GetCelebrityById(int Id);                      // получить Знаменитость по Id
    bool DelCelebrity(int id);                        // удалить Знаменитость по Id
    bool AddCelebrity(T celebrity);                   // добавить Знаменитость
    bool UpdCelebrity(int id, T celebrity);           // изменить Знаменитость по Id
    int GetCelebrityIdByName(string name);            // получить первый Id по вхождению подстроки
}
public interface ILifeevent<T> : IDisposable
{
    List<T> GetAllLifeevents();                       // получить все События
    T? GetLifeevetById(int Id);                       // получить Событие по Id
    bool DelLifeevent(int id);                        // удалить Событие по Id
    bool AddLifeevent(T lifeevent);                   // добавить Событие
    bool UpdLifeevent(int id, T lifeevent);           // изменить Событие по Id
}
```
> **Опечатка в интерфейсе (сохранять как в ТЗ!):** `GetLifeevetById` — без буквы «n».
> В приложении-тесте используется именно `repo.GetLifeevetById(id)`.

Уточнение интерфейса репозитория (стр. 3 PDF):
```csharp
public interface IRepository : DAL_Celebrity.IRepository<Celebrity, Lifeevent> { }
```

### Repository и Context (Entity Framework) — стр. 3
```csharp
public class Repository : IRepository
{
    Context context;
    public Repository() { this.context = new Context(); }
    public Repository(string connectionstring) { this.context = new Context(connectionstring); }
    public static IRepository Create() { return new Repository(); }
    public static IRepository Create(string connectionstring) { return new Repository(connectionstring); }

    public List<Celebrity> getAllCelebrities() { return this.context.Celebrities.ToList<Celebrity>(); }
    public Celebrity? GetCelebrityById(int Id) {...}
    public bool AddCelebrity(Celebrity celebrity) {...}
    public bool DelCelebrity(int id) {...}
    public bool UpdCelebrity(int id, Celebrity celebrity) {...}
    public List<Lifeevent> GetAllLifeevents() { return this.context.Lifeevents.ToList<Lifeevent>(); }
    public Lifeevent? GetLifeevetById(int Id) {...}
    public bool AddLifeevent(Lifeevent lifeevent) {...}
    public bool DelLifeevent(int id) {...}
    public bool UpdLifeevent(int id, Lifeevent lifeevent) {...}
    public List<Lifeevent> GetLifeeventsByCelebrityId(int celebrityId) {...}
    public Celebrity? GetCelebrityByLifeeventId(int lifeeventId) {...}
    public int GetCelebrityIdByName(string name) {...}
    public void Dispose() {...}
}

public class Context : DbContext
{
    public string ConnectionString { get; private set; } = null;
    public Context(string connstring) : base()
    {
        this.ConnectionString = connstring;
        //this.Database.EnsureDeleted();
        //this.Database.EnsureCreated();
    }
    public Context() : base()
    {
        //this.Database.EnsureDeleted();
        //this.Database.EnsureCreated();
    }
    public DbSet<Celebrity> Celebrities { get; set; }
    public DbSet<Lifeevent> Lifeevents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (this.ConnectionString is null) this.ConnectionString = @"Data source=[ЗАКРАШЕНО];...";
        optionsBuilder.UseSqlServer(this.ConnectionString);
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Celebrity>() ...   // закомментированный код настройки полей (IsRequired, HasMaxLength и т.п.)
        modelBuilder.Entity<Lifeevent>() ...
        base.OnModelCreating(modelBuilder);
    }
}
```
**Строка подключения на скриншоте закрашена** — задаётся студентом. Видимая часть:
`@"Data source=...` и далее `TrustServerCertificate=True;User Id=...;Password=...`.

Реальная строка подключения из прилагаемого `Лабораторная_06_ASPNETCORE_Init.cs`:
```csharp
static string connstring = @"Data source=172.16.193.88; Initial Catalog=LES01;" +
                            @"TrustServerCertificate=True;User Id=smw60;Password=21625";
```

### Класс Init (прилагается файлом Init.cs)
```csharp
public class Init
{
    public Init() { }
    public Init(string conn) { connstring = conn; }
    public static void Execute(bool delete = true, bool create = true)
    {
        Context context = new Context(connstring);
        if (delete) context.Database.EnsureDeleted();   // если есть БД, то она удаляется
        if (create) context.Database.EnsureCreated();   // если нет БД, то создается
        // ... добавление 10 знаменитостей (Chomsky, Berners-Lee, Codd, Knuth,
        //     Torvalds, Neumann, Dijkstra, Lovelace, Babbage, Tanenbaum)
        //     и по 2 Lifeevent на каждого (Дата рождения + событие) ...
        context.SaveChanges();
    }
}
```
Знаменитости инициализации (Id 1..10): Noam Chomsky (US), Tim Berners-Lee (UK), Edgar Codd (US),
Donald Knuth (US), Linus Torvalds (US), John Neumann (US), Edsger Dijkstra (NL), Ada Lovelace (UK),
Charles Babbage (UK), Andrew Tanenbaum (NL).
События: например для Chomsky — 1928-12-07 «Дата рождения» и 1955-01-01
«Издание книги "Логическая структура лингвистической теории"».

## Задание 3 (♣) — тест репозитория
5. Разработайте консольное приложение **DAL_Celebrity_MSSQL_Test** для тестирования репозитория
   (из DAL_Celebrity_MSSQL).
6. Программный код теста (файл **Programm.cs** прилагается).
7. Результаты теста.
8. Проверьте с помощью теста работоспособность функций репозитория (DAL_Celebrity_MSSQL)
   и добейтесь успешного выполнения теста.

### Код теста (прилагается файлом `Лабораторная_06_ASPNETCORE_Program.cs`)
```csharp
string CS = @"Data source=172.16.xx.xx; Initial Catalog=LES01;TrustServerCertificate=True;User Id=xxxx;Password=xxxxxxxxx";

Init init = new Init(CS);
Init.Execute(delete: true, create: true);

Func<Celebrity, string> printC = (c) => $"Id = {c.Id}, FullName = {c.FullName}, Nationality = {c.Nationality}, ReqPhotoPath = {c.ReqPhotoPath}";
Func<Lifeevent, string> printL = (l) => $"Id = {l.Id}, CelebrityId = {l.CelebrityId}, Date = {l.Date}, Description = {l.Description}, ReqPhotoPath = {l.ReqPhotoPath}";

using (IRepository repo = Repository.Create(CS))
{
    repo.GetAllCelebrities().ForEach(celebrity => Console.WriteLine(printC(celebrity)));
    repo.GetAllLifeevents().ForEach(life => Console.WriteLine(printL(life)));
    Celebrity c = new Celebrity { FullName = "Albert Einstein", Nationality = "DE", ReqPhotoPath = "Einstein.jpg" };
    if (repo.AddCelebrity(c)) Console.WriteLine($"OK: AddCelebrity: {printC(c)}");
    // DelCelebrity, UpdCelebrity, AddLifeevent, DelLifeevent, UpdLifeevent,
    // GetLifeeventsByCelebrityId, GetCelebrityByLifeeventId
}
```

Результаты теста (скриншот, стр. 6): консоль печатает `------ GetAllCelebrities() -------------`
с 6 знаменитостями, `------ GetAllLifeevents() -------------`, `OK: AddCelebrity`,
`OK: DelCelebrity`, `OK: UpdCelebrity`, `OK: AddLifeevent`, `OK: DelLifeevent`, `OK: UpdLifeevent`,
`OK: GetLifeeventsByCelebrityId`, `OK: GetCelebrityByLifeeventId`.

## Задание 4 — ASPA006_1
9. Фотографии размещены на **diskstation (student/fitfit)**.
10. Разработайте ASPA. Имя решения: ASPA, имя проекта: **ASPA006_1**. Версия .NET 7.0 или 8.0.
11. Подключите библиотеку **DAL_Celebrity_MSSQL** к **ASPA006_1**.
12. Создайте конфигурационный файл **`Celebrities.config.json`**, позволяющий параметризировать
    директорий для фотографий и строку подключения к серверу СУБД.
13. Обеспечьте доступ к конфигурации (строка подключения и папка для фотографий) с помощью
    объекта класса **`CelebritiesConfig`** интерфейса **`IOptions`**.

### Формат конфигурационного файла (из лабы 7, где он приведён полностью)
```json
// Celebrities.config.json
// -- PhotosRequestPath  - префикс в URI запроса
// -- PhotosFolder       - директория с файлами
// -- ConnectionString   - строка подключения к БД
"Celebrities": {
    "PhotosRequestPath": "/Photos",
    "PhotosFolder":       "D:\\VS_Projects\\ANC\\ANC25\\WEBAPI_DLL\\Photos",
    "ConnectionString":   "Data source=172.16.193.88; Initial Catalog=LES28X;TrustServerCertificate=True;User Id=...;Password=...;"
}
```
> Внимание: `PhotosRequestPath` начинается со **слеша** (`/Photos`) — в лабе 7 это исправленный
> вариант (в лабе 8 в конфиге он уже с `/Photos`), а в первоначальном варианте лабы 6 на скриншоте
> указано `"/Photos"`. Используйте вариант со слешем.

14. Создайте **Soped-сервис** (Scoped service), реализующий интерфейс `IRepository`
    (задание 2), как в следующем примере:

### Регистрация Scoped-сервиса (задание 14) — со скриншота
```csharp
builder.Services.AddScoped<IRepository, Repository>((IServiceProvider p) => {
    CelebritiesConfig config = p.GetRequiredService<IOptions<CelebritiesConfig>>().Value;
    return new Repository(config.ConnectionString);
});
```
Подчёркнуто красным (это и надо пояснить): `IRepository`, `Repository`,
`IOptions<CelebritiesConfig>`, `config.ConnectionString`.

Для чтения конфигурации (тип `CelebritiesConfig` с секцией `Celebrities`) — стандартный
подход .NET: `builder.Services.Configure<CelebritiesConfig>(builder.Configuration.GetSection("Celebrities"));`
и получение через `IOptions<CelebritiesConfig>`.

15. Разработайте следующие конечные точки, используя сервис, реализующий `IRepository`:

### Конечные точки (задание 15) — читаемая часть со скриншота, тела `{...}` закрыты
```csharp
//---------------- ЗНАМЕНИТОСТИ (Celebrities) -----------------------------
var celebrities = app.MapGroup("/api/Celebrities");
//  все знаменитости
celebrities.MapGet("/", (IRepository repo) => repo.GetAllCelebrities());
//  знаменитость по ID
celebrities.MapGet("/{id:int:min(1)}", (IRepository repo, int id) => {...});
//  знаменитость по ID события
celebrities.MapGet("/Lifeevents/{id:int:min(1)}", (IRepository repo, int id) => {...});
//  удалить знаменитость по ID
celebrities.MapDelete("/{id:int:min(1)}", (IRepository repo, int id) => {...});
//  добавить новую знаменитость
celebrities.MapPost("/", (IRepository repo, Celebrity celebrity) => {...});
//  изменить знаменитость по ID
celebrities.MapPut("/{id:int:min(1)}", (IRepository repo, int id, Celebrity celebrity) => {...});
//  получить файл фотографии по имени файла (fname)
celebrities.MapGet("/photo/{fname}", async (IOptions<CelebritiesConfig> iconfig, HttpContext context, string fname) => {...});

//-------------------- СОБЫТИЯ (Lifeevents) --------------------------------
var lifeevents = app.MapGroup("/api/Lifeevents");
//  все события
lifeevents.MapGet("/", (IRepository repo) => repo.GetAllLifeevents());
//  событие по ID
lifeevents.MapGet("/{id:int:min(1)}", (IRepository repo, int id) => {...});
//  все события по ID знаменитости
lifeevents.MapGet("/Celebrities/{id:int:min(1)}", (IRepository repo, int id) => {...});
//  удалить событие по ID
lifeevents.MapDelete("/{id:int:min(1)}", (IRepository repo, int id) => {...});
//  добавить новое событие
lifeevents.MapPost("/", (IRepository repo, Lifeevent lifeevent) => {...});
//  изменить событие по ID
lifeevents.MapPut("/{id:int:min(1)}", (IRepository repo, int id, Lifeevent lifeevent) => {...});
```

Обратите внимание на констрейнты `int:min(1)` — идентификатор должен быть целым и >= 1.

16. Для обработки ошибок в конечных точках разработайте **middleware-обработчик ошибок**.
17. Тест (файл **app.http**, прилагается) для конечных точек.

### Файл app.http (прилагается) — полное содержание
```
###
@hp=http://localhost:5204
@capi=/api/Celebrities
###

### все знаменитости
### celebrities.MapGet("/",
GET {{hp}}{{capi}}

### знаменитость по ID
### MapGet("/{id:int:min(1)}"
GET {{hp}}{{capi}}/7

### знаменитость по ID события
### MapGet("/Lifeevents/{id:int:min(1)}"
GET {{hp}}{{capi}}/Lifeevents/7

### удалить знаменитость по ID
###.MapDelete("/{id:int:min(1)}",
DELETE {{hp}}{{capi}}/4

### добавить новую знаменитость
###  MapPost("/",
POST {{hp}}{{capi}}
Content-Type:application/json

{
    "fullName":    "Ершов Андрей",
    "nationality": "RU",
    "reqPhotoPath":"Ershov.jpg"
}

### изменить знаменитость по ID
###  MapPut("/{id:int:min(1)}",
PUT {{hp}}{{capi}}/13
Content-Type:application/json

{
    "fullName":    "Ершов Андрей Петрович",
    "nationality": "RU",
    "reqPhotoPath":"Ershov.jpg"
}

### получить файл фотографии по имени файла (fname)
### MapGet("/photo/{fname}"
GET {{hp}}{{capi}}/photo/Ershov.jpg

### @lapi=/api/Lifeevents

### все события
###  MapGet("/",
GET {{hp}}{{lapi}}

### событие по ID
###  MapGet("/{id:int:min(1)}"
GET {{hp}}{{lapi}}/10

### все события по ID знаменитости
###  MapGet("/Celebrities/{id:int:min(1)}
GET {{hp}}{{lapi}}/Celebrities/3

### удалить событие по ID
###  MapDelete("/{id:int:min(1)}"
DELETE {{hp}}{{lapi}}/10

### добавить новое событие
###  MapPost("/",
POST {{hp}}{{lapi}}
Content-Type:application/json

{
    "celebrityId": 13,
    "date": "1931-04-19T00:00:00",
    "description": "Дата рождения",
    "reqPhotoPath": null
 }

### изменить событие по ID
###  MapPut("/{id:int:min(1)}"
PUT {{hp}}{{lapi}}/24
Content-Type:application/json

{
    "celebrityId": 13,
    "date": "1931-04-19T00:00:00",
    "description": "Дата рождения. Москва.",
    "reqPhotoPath": null
}
```

### Ожидаемые ответы (скриншоты PDF)

GET `/api/Celebrities` -> массив 6 знаменитостей:
```json
[{"id":1,"fullName":"Noam Chomsky","nationality":"US","reqPhotoPath":"Chomsky.jpg"},
 {"id":2,"fullName":"Tim Berners-Lee","nationality":"UK","reqPhotoPath":"Berners-Lee.jpg"},
 {"id":3,"fullName":"Edgar Codd","nationality":"US","reqPhotoPath":"Codd.jpg"},
 {"id":4,"fullName":"Donald Knuth","nationality":"US","reqPhotoPath":"Knuth.jpg"},
 {"id":5,"fullName":"Linus Torvalds","nationality":"US","reqPhotoPath":"Linus.jpg"},
 {"id":6,"fullName":"John Neumann","nationality":"US","reqPhotoPath":"Neumann.jpg"}]
```
GET `/api/Celebrities/7`:
```json
{"id":7,"fullName":"Edsger Dijkstra","nationality":"NL","reqPhotoPath":"Dijkstra.jpg"}
```
GET `/api/Celebrities/Lifeevents/7` -> знаменитость `Donald Knuth` (id 4).
POST `/api/Celebrities` (Ершов Андрей, RU, Ershov.jpg):
```json
{"id": 13, "fullName": "Ершов Андрей", "nationality": "RU", "reqPhotoPath": "Ershov.jpg"}
```
DELETE несуществующей знаменитости -> **404**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "#404002:Celebrity Id = 4",
  "instance": "ANC25"
}
```
DELETE несуществующего события -> **404**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "#404004:Lifeevent Id = 10",
  "instance": "ANC25"
}
```

События в БД (для проверки): id 1 (celebrityId 1, 1928-12-07, Дата рождения), id 2 (celebrityId 1,
1955-01-01, «Издание книги "Логическая структура лингвистической теории"»), id 3 (celebrityId 2,
1955-06-08, Дата рождения), id 4 (celebrityId 2, 1989-06-08, «В CERN предложил "Гиппертекстовый проект"»),
id 5 (celebrityId 3, 1923-08-23, Дата рождения), id 6 (celebrityId 3, 2003-04-18, Дата смерти),
id 10 (celebrityId 5, 1991-09-17, «Выложил исходный код OS Linus (версии 0.01)»).

## Задание по Index.html
18. Разработайте html-страницу **Index.html** указанного вида. Обеспечьте отображение
    фотографий знаменитостей (используйте endpoint `/api/Celebrities/photo/{fname}`).
    Обеспечьте отображение этой страницы при запуске приложения **по умолчанию**.
19. При одинарном клике мышью фотографии внизу отображаются строки с данными о **событиях**.

## Ключевые API и типы

| Имя | Назначение |
|---|---|
| `DbContext`, `DbSet<T>`, `ModelBuilder` | Entity Framework Core |
| `OnConfiguring`, `OnModelCreating` | настройка подключения и модели |
| `UseSqlServer(connectionString)` | провайдер MS SQL Server |
| `Database.EnsureDeleted()`, `EnsureCreated()` | пересоздание БД (для тестов) |
| `IRepository<T1,T2>`, `IMix`, `ICelebrity<T>`, `ILifeevent<T>` | интерфейсы DAL |
| `IDisposable` | освобождение ресурсов (DbContext) |
| `AddScoped<IRepository, Repository>(factory)` | регистрация Scoped-сервиса |
| `IServiceProvider.GetRequiredService<T>()` | разрешение зависимости |
| `IOptions<T>`, `IOptions<T>.Value` | доступ к конфигурации |
| `CelebritiesConfig` | класс конфигурации (секция `Celebrities`) |
| `MapGroup("/api/Celebrities")` | группа маршрутов знаменитостей |
| `int:min(1)` | констрейнт: целое >= 1 |
| `IExceptionHandlerFeature`, `UseExceptionHandler` | обработка исключений (Лекция 06) |
| `Results.NotFound/Ok/Problem/BadRequest` | формирование ответов |
| `HttpContext`, `PhysicalFile`, `FileStreamResult` | отдача файла фотографии |
| `GetLifeevetById` | ⚠ опечатка в интерфейсе (без «n») |

## Ответы для защиты
В ТЗ лабы 6 отдельного раздела «Ответьте на вопросы» нет. Вопросы к защите по этой работе
(определяются по аналогии с предыдущими лабами и содержанию лекций 05-06):
- в чём различие `ICelebrity<T>` / `ILifeevent<T>` / `IMix<T1,T2>` и почему `IRepository<T1,T2>`
  наследует все три;
- зачем `IOptions<CelebritiesConfig>` вместо чтения конфигурации напрямую;
- чем Scoped-жизненный цикл отличается от Singleton/Transient;
- как middleware-обработчик ошибок формирует ProblemDetails (#404002, #404004, ANC25);
- как работает `{id:int:min(1)}`.

## Источник
- `labs/Лабораторная_06_ASPNETCORE_API/Лабораторная_06_ASPNETCORE_API.pdf` — стр. 1 (схема БД LES,
  классы Celebrity/Lifeevent, задание 1), стр. 2 (интерфейсы DAL_Celebrity, задание 2),
  стр. 3 (IRepository : DAL_Celebrity.IRepository<...>, Repository, Context; задание 4),
  стр. 4 (скриншот интерфейса/структуры), стр. 5-6 (задание 3, результаты теста),
  стр. 7 (задание 4, п.9-13, скриншот Index.html), стр. 8 (п.14 регистрация Scoped-сервиса,
  п.15-17 конечные точки), стр. 9-13 (app.http: GET/DELETE/POST/PUT знаменитостей и событий,
  скриншоты ответов 404), стр. 14-15 (задание Index.html, п.18-19).
- Прилагаемые файлы в `labs/Лабораторная_06_ASPNETCORE_API/`:
  `Лабораторная_06_ASPNETCORE_DAL_Celebrity.cs` (интерфейсы),
  `Лабораторная_06_ASPNETCORE_Init.cs` (класс Init + строка подключения),
  `Лабораторная_06_ASPNETCORE_Program.cs` (код теста репозитория),
  `Лабораторная_06_ASPNETCORE_app.http` (файл запросов),
  `Celebrities/Celebrities.json` + фотографии.
- `labs/Лабораторная_07_ASPNETCORE_Page.pdf` — стр. 1 (формат `Celebrities.config.json`).
- `lectures/Лекция_06_ASPNETCORE_MIDDLEWARE_ERRORHANDLER.pdf` — стр. 1-5 (обработка ошибок,
  коды 404/409/500, `UseExceptionHandler`, `UseMiddleware<>`).