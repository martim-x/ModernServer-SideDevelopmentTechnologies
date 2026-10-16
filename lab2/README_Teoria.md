# Обобщённая теория: Middleware, конвейер и обработка ошибок в ASP.NET Core

## Что было проделано (общий план работы)

Работа охватывает три приложения (`ASPA002_1`, `ASPA002_2`, `ASPA002_3`), построенные по единому шаблону «пустой ASP.NET Core» (`dotnet new web`, .NET 8). Общая архитектурная линия — построение конвейера обработки HTTP-запросов из встроенных middleware-компонентов и конечных точек (endpoint), с последующим добавлением пользовательской обработки исключений.

---

## 1. Middleware — общее определение и типы

### Определение

**Middleware** — компонент конвейера (`request pipeline`), который:

- принимает `HttpContext` (запрос + ответ);
- решает: обработать запрос сам или передать дальше (`await next(context)`);
- может работать **до** (`before`) и **после** (`after`) следующего компонента (модель «луковицы» / onion model).

### Типы middleware (по способу подключения)

| Тип | Как подключается | Пример из работы |
|---|---|---|
| **Встроенный (built-in)** | `app.Use<Name>()` | `UseWelcomePage`, `UseStaticFiles`, `UseDefaultFiles`, `UseExceptionHandler` |
| **Inline (лямбда)** | `app.Use(async (context, next) => ...)` | не использовался в лабе, но показан в лекции |
| **Класс (`UseMiddleware<T>`)** | `app.UseMiddleware<MyMiddleware>(params)` | `MiddlewareErrorHandler` (лекция 6) |
| **Расширение (`IApplicationBuilder`)** | `app.UseCustomName(prefix)` | `UseANCErrorHandler` (лекция 6) |

---

## 2. Можно ли считать `MapGet` middleware?

**Нет, нельзя.**

Различие принципиальное:

|| Middleware (`Use*`) | Endpoint (`Map*`) |
|---|---|---|
| **Что делает** | оборачивает следующий обработчик (`next`) | регистрирует конечную точку (делегат) |
| **Передаёт дальше?** | да — через `await next(context)` | нет — это терминальный компонент |
| **Выбирается по маршруту?** | нет | да — через `UseRouting` |
| **Имеет метаданные?** | нет | да (`Endpoint.Metadata`) |

`MapGet` — это метод `EndpointRouteBuilder`, который добавляет **endpoint**, а не middleware. Он выполняется в конце конвейера (`UseEndpoints`), а не внутри цепочки middleware.

> **Практическое правило из работы:** в `ASPA002_1` вызов `app.MapGet("/aspnetcore", ...)` ниже `UseWelcomePage()` не сработает никогда, потому что `WelcomePageMiddleware` — терминальный компонент: при совпадении пути он формирует ответ сам и **не вызывает `next()``. Управление до `MapGet` просто не доходит.

---

## 3. Порядок регистрации — критичен

Конвейер строится один раз (`builder.Build()` → связный список делегатов). Порядок вызовов в коде = порядок выполнения.

### Правильный порядок (из работы)

```csharp
app.UseExceptionHandler("/error");   // 1. обработка ошибок — первыми
app.UseStaticFiles();                  // 2. статика — рано (разрыв цепочки)
app.UseRouting();                      // 3. маршрутизация
app.MapGet("/", ...);                   // 4. endpoint'ы — в конце
```

### Ключевые правила из практики

- `UseExceptionHandler` / `UseDeveloperExceptionPage` — **первыми**, иначе исключения внутри других middleware останутся необработанными.
- `UseDefaultFiles()` — **до** `UseStaticFiles()`: иначе подстановка `/` → `/index.html` не сработает (статический middleware уже попытался найти файл `/`).
- `UseStaticFiles()` — **рано**, чтобы разорвать цепочку при совпадении (экономия работы следующих компонентов).
- `UseAuthentication` → `UseAuthorization` → endpoint'ы (нужен выбранный endpoint для проверки прав).

---

## 4. Паттерны, встречающиеся в ASP.NET Core

### Builder (`WebApplicationBuilder`)

```csharp
var builder = WebApplication.CreateBuilder(args);   // построитель
builder.Services.AddControllers();                  // конфигурация
var app = builder.Build();                           // сборка продукта
```

Это классический паттерн **Builder**: пошаговая сборка сложного объекта (`WebApplication`) из простых операций. В работе используется в каждом проекте.

### Chain of Responsibility (`middleware pipeline`)

Каждый middleware — обработчик цепочки, который решает: обработать самому или передать `next`. Это прямо соответствует паттерну **Chain of Responsibility**.

---

## 5. Статические файлы и каталог `wwwroot`

- **Web root** по умолчанию: `{CONTENT ROOT}/wwwroot`.
- `UseStaticFiles()` отдаёт файлы из `WebRootPath` (по умолчанию `wwwroot`).
- `UseDefaultFiles()` — **переписывает URL**, не отдаёт файл сам. Поэтому стоит **перед** `UseStaticFiles()`.
- Для дополнительного каталога (`Picture` в `ASPA002_2`) используется `PhysicalFileProvider` с `RequestPath = "/static"`.

---

## 6. Обработка исключений — схема и ограничения

### Как работает `UseExceptionHandler`

1. Оборачивает весь конвейер в `try/catch`.
2. При исключении: меняет `Request.Path` на `/error`, ставит код **500**, очищает ответ (`Response.Clear()`), сбрасывает endpoint/route values.
3. Сохраняет исходное исключение в `IExceptionHandlerFeature` (доступно в `/error` через `context.Features.Get<IExceptionHandlerFeature>()`).
4. **Перезапускает конвейер** с новым путём `/error`.
5. В обработчике `/error` выводится `<h1>Oops!</h1>` и логируется через `ILogger<Program>`.

### Важные ограничения

- В среде `Development` автоматически добавляется `UseDeveloperExceptionPage` **первым** — он перехватывает исключение раньше `UseExceptionHandler`. Поэтому для проверки собственного обработчика нужно запускать в `Production` (или `Release` без `Development` в `launchSettings`).
- Если ответ уже начал отправляться (`Response.HasStarted`), обработчик ничего не может сделать — исключение пробрасывается дальше.
- `scoped`-сервисы остаются теми же при повторном входе в конвейер.

---

## 7. Проверенные результаты работы

|| Приложение | Порт | Режим | Проверенные запросы | Результат |
|---|---|---|---|---|---|
| `ASPA002_1` | `ASPA002_1` | 5001 | `Development` | `/aspnetcore` → 200 (WelcomePage); `/` → 200 (`index.html`); `/index.html` → 200; `/css/index.css` → 200 | Все работает |
| `ASPA002_2` | `ASPA002_2` | 5002 | `Development` | `/` → 200 (`Neumann.html`); `/Neumann.html` → 200; `/index.html` → 404; `/static/Picture/Neumann.jpg` → 200 (`image/jpeg`) | Все работает |
| `ASPA002_3` | `ASPA002_3` | 5003 | `Release` + `Production` | `/` → 200 (`"Start"`); `/test1`, `/test2`, `/test3` → 500 (`<h1>Oops!</h1>`); `/error` → 200 (`<h1>Oops!</h1>`); консоль: `ExceptionHandler` + стек | Все работает |

---

## 8. Источники

- Документация Microsoft Learn: [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/)
- [Write custom middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write) — требования к классу (`Invoke`/`InvokeAsync`, `RequestDelegate`)
- [Serve static files](https://learn.microsoft.com/aspnet/core/fundamentals/static-files) — `wwwroot`, `PhysicalFileProvider`, `UseFileServer`
- [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling) — `UseExceptionHandler`, `IExceptionHandlerFeature`
- [WebApplication и WebApplicationBuilder](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/webapplication) — автоматически добавляемые middleware
