# Лабораторная работа № 2 — ASPA (приложение ASP.NET Core)

**Дисциплина:** Современные технологии серверной разработки
**Тема:** статические файлы и обработка исключений в ASP.NET Core
**Цель:** изучить встроенные middleware-элементы `UseWelcomePage`, `UseDefaultFiles`, `UseStaticFiles`, `UseExceptionHandler`.

Решение **ASPA** содержит три проекта, созданных шаблоном «Пустой шаблон ASP.NET Core» (`dotnet new web`), версия .NET 8:

| Проект     | Порт   | Содержание                                                            |
|------------|--------|-----------------------------------------------------------------------|
| `ASPA002_1`| 5001   | Приветственная страница ASP.NET Core и статические файлы из `wwwroot`  |
| `ASPA002_2`| 5002   | Дополнительный каталог статических файлов `Picture` на конечной точке `/static` |
| `ASPA002_3`| 5003   | Обработка исключений встроенным middleware-элементом `UseExceptionHandler` |

## Запуск

```bash
dotnet run --project ASPA002_1        # http://localhost:5001
dotnet run --project ASPA002_2        # http://localhost:5002
dotnet run --project ASPA002_3 -c Release   # http://localhost:5003 (обязательно Release)
```

Пороли заданы в `Properties/launchSettings.json` каждого проекта, чтобы приложения не конфликтовали.

---

## Задание 1 (ASPA002_1)

### Пункт 2. Конечная точка `/aspnetcore` и middleware-механизм

```csharp
app.Map("/aspnetcore", branch => branch.UseWelcomePage());
```

`UseWelcomePage` — встроенный middleware-элемент, отображающий приветственную страницу ASP.NET Core.

**Механизм middleware.** Обработка запроса выполняется по цепочке (конвейеру) middleware-компонентов.
Каждый компонент:

1. получает объект `HttpContext` (запрос и ответ);
2. либо самостоятельно формирует ответ и **не** вызывает `await next(context)` — конвейер на этом завершается;
3. либо вызывает `await next(context)` и передаёт управление следующему компоненту цепочки.

Компоненты подключаются по порядку, и порядок принципиален. На один HTTP-запрос приходится
ровно один ответ, поэтому сформировавший ответ компонент завершает цепочку.

**Почему следующий ниже по коду `MapGet("/aspnetcore", …)` неработоспособен.**
Запрос `/aspnetcore` попадает в ветку, подключённую `app.Map`, где `UseWelcomePage` сам формирует
ответ и не вызывает `next()`. Конвейер на этом обрывается, и до `MapGet` управление не доходит —
обработчик не будет вызван никогда (фактически он недостижим).

### Пункт 3 и 5. Статические файлы и стартовая страница

```csharp
app.UseDefaultFiles();   // подставляет имя стартовой страницы (index.html)
app.UseStaticFiles();    // отдаёт статические файлы каталога wwwroot
```

`UseDefaultFiles` должен подключаться **перед** `UseStaticFiles`, иначе подстановки стартовой
страницы не произойдёт. В результате запрос к `/` возвращает содержимое `wwwroot/index.html`.

### Пункт 4. Файлы страницы

```
ASPA002_1/wwwroot/index.html      разметка страницы (стили, скрипты, фотография)
ASPA002_1/wwwroot/css/index.css   оформление: img{}, .red, .green, .magenta, .blue
ASPA002_1/wwwroot/js/calc.js      функции Sum, Sub, Mul, Div
ASPA002_1/wwwroot/jpg/Neumann.jpg фотография Джона фон Неймана (2000x1020)
```

Проверенный результат запроса к `/` в браузере:

```
Calc
Sum(7, 3) = 10      (красным)
Sub(7, 3) = 4       (зелёным)
Mul(7, 3) = 21      (пурпурным)
Div(7, 3) = 2.33    (синим)
[фотография Джона фон Неймана во всю ширину страницы]
```

---

## Задание 2 (ASPA002_2)

Приложение аналогично `ASPA002_1`, с изменениями:

1. **Стартовая страница переименована в `Neumann.html`.** Изменён список имён стартовых страниц:

   ```csharp
   app.UseDefaultFiles(new DefaultFilesOptions
   {
       DefaultFileNames = new List<string> { "Neumann.html" }
   });
   ```

2. **Конечная точка `/static`.** К ней подключён встроенный middleware-элемент `UseStaticFiles`
   с дополнительным каталогом статических файлов `Picture`:

   ```csharp
   app.UseStaticFiles(new StaticFileOptions
   {
       FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
       RequestPath = "/static"
   });
   ```

3. **Дополнительный каталог `Picture`** создан рядом с `wwwroot`, в него перенесён `Neumann.jpg`.
   Корень `PhysicalFileProvider` — каталог проекта, поэтому файл из `Picture` публикуется
   по адресу `/static/Picture/Neumann.jpg`.

4. **В `Neumann.html` исправлен путь к фотографии:** `src="/static/Picture/Neumann.jpg"`.

Проверено: `/` и `/Neumann.html` → 200, `/index.html` → 404 (файл переименован),
`/static/Picture/Neumann.jpg` → 200 `image/jpeg`, 321230 байт.

---

## Задание 3 (ASPA002_3)

Сервер имеет 5 конечных точек. Запросы к трём из них приводят к исключению:

| Конечная точка | Исключение                                |
|----------------|-------------------------------------------|
| `/test1`       | пользовательское `Exception`              |
| `/test2`       | `DivideByZeroException` (деление на 0)     |
| `/test3`       | `IndexOutOfRangeException` (выход за границы массива) |
| `/`            | —                                         |
| `/error`       | обработчик исключений                     |

### Логика работы

1. `app.UseExceptionHandler("/error")` — встроенный middleware-элемент. Он перехватывает
   необработанное исключение, выполняет повторный проход конвейера по адресу `/error`
   и возвращает ответ с кодом **500**. Исходное исключение при этом доступно через
   `context.Features.Get<IExceptionHandlerFeature>()`.
2. `/error` с помощью `WriteAsync` выводит страницу `<h1>Oops!</h1>`.
3. `logger.LogError(exobj?.Error, "ExceptionHandler")` записывает в журнал сообщение
   `ExceptionHandler` и само исключение со стеком вызовов.
4. `builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.None)` отключает
   служебные сообщения диагностики, чтобы в консоли остались только сообщения приложения.

Фактический журнал консоли при запросах к `/test1`, `/test2`, `/test3`:

```
fail: Program[0]
      ExceptionHandler
      System.Exception: -- Exception Test --
         at Program.<>c.<Main>b__0_1() in .../ASPA002_3/Program.cs:line 30
         at lambda_method2(Closure, Object, HttpContext)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.Invoke(HttpContext context)
fail: Program[0]
      ExceptionHandler
      System.DivideByZeroException: Attempted to divide by zero.
         at lambda_method3(Closure, Object, HttpContext)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.Invoke(HttpContext context)
fail: Program[0]
      ExceptionHandler
      System.IndexOutOfRangeException: Index was outside the bounds of the array.
         at Program.<>c.<Main>b__0_3() in .../ASPA002_3/Program.cs:line 40
         at lambda_method4(Closure, Object, HttpContext)
         at Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddlewareImpl.Invoke(HttpContext context)
```

Фактические ответы (режим Release):

| Запрос     | Код | Тело ответа       |
|------------|-----|-------------------|
| `/`        | 200 | `Start`           |
| `/test1`   | 500 | `<h1>Oops!</h1>`  |
| `/test2`   | 500 | `<h1>Oops!</h1>`  |
| `/test3`   | 500 | `<h1>Oops!</h1>`  |
| `/error`   | 200 | `<h1>Oops!</h1>`  |

### Пункт 14. Режим Release

Обработчик проверен в режиме Release (`dotnet run -c Release`), среда `ASPNETCORE_ENVIRONMENT = Production`.
Дополнительно проверено и в режиме Debug со средой `Development`: явный `UseExceptionHandler`
в конвейере обрабатывает исключение и там, поэтому страница `Oops!` и запись `ExceptionHandler`
в журнале выводятся в обоих режимах. Условие задания о режиме Release соблюдается.

---

## Задание 4 (вопросы 15–22)

Ответы на вопросы 15–22 (паттерн Builder, Chain of Responsibility, Middleware, статический файл,
признак наименования middleware, встроенные middleware, каталог статических файлов,
Джон фон Нейман) оформлены отдельно в `lab2_ответы.md`.


---

## Обобщённая теория (дополнение к заданию)

Что было проделано (общий план работы)

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


---

## Ответы на вопросы (дополнение)


---

## 1. Типы middleware

| Тип | Признак | Пример из работы |
|---|---|---|
| **Встроенный (built-in)** | метод `Use<Name>()` из `Microsoft.AspNetCore.Builder` | `UseWelcomePage`, `UseStaticFiles`, `UseDefaultFiles`, `UseExceptionHandler` |
| **Inline (лямбда)** | `app.Use(async (context, next) => ...)` | не использован в лабораторной, но показан в лекции 6 (`try/catch` с `await next()`) |
| **Класс (`IMiddleware` / `UseMiddleware<T>`)** | публичный конструктор с `RequestDelegate`, метод `Invoke` или `InvokeAsync` | `MiddlewareErrorHandler` (лекция 6, слайд 4) |
|| **Промежуточный (intermediate)** | `app.Use(...)` или `UseMiddleware<T>` — проходит через цепочку, не разрывает её | `UseExceptionHandler`, `UseRouting`, `UseStaticFiles` (когда файл не найден) |
|| **Расширение (`IApplicationBuilder`)** | собственный метод `Use<Name>(this IApplicationBuilder, ...)` | `UseANCErrorHandler` (лекция 6) |

> **Критерий различия:** middleware всегда принимает `RequestDelegate next` (следующий обработчик в цепочке) и возвращает `Task`. Endpoint (`Map*`) — это делегат, который выполняется в конце конвейера (`UseEndpoints`) и не принимает `next`.

---

## 2. Можно ли считать middleware `MapGet`?

**Нет.**

### Почему это принципиально

- `MapGet` — метод `EndpointRouteBuilder`. Он **регистрирует endpoint**, не подключает middleware.
- Middleware работает через `await next(context)` — передаёт управление дальше. `MapGet` — терминальный компонент: он формирует ответ сам и **не вызывает ничего дальше**.
- `UseWelcomePage()` в `ASPA002_1` — именно middleware, и именно поэтому `MapGet("/aspnetcore", ...)` ниже по коду **недостижим**: `WelcomePageMiddleware` при совпадении пути формирует ответ сам и не вызывает `_next()`.

### Проверка из практики (задание 1, `ASPA002_1`)

До исправления в `Program.cs` было:

```csharp
app.Map("/aspnetcore", branch => branch.UseWelcomePage());
// ... (сломанная структура)
```

После исправления структура правильная:

```csharp
app.Map("/aspnetcore", branch => branch.UseWelcomePage()); // middleware — терминальный
app.UseDefaultFiles();    // middleware — переписывает URL
app.UseStaticFiles();     // middleware — отдаёт файл (тоже терминальный при совпадении)
app.Run();                // терминальный делегат
```

> `MapGet` — это endpoint, а не middleware. Разница видна в типах: `MapGet` возвращает `IEndpointConventionBuilder`, `Use*` возвращает `IApplicationBuilder`.

---

## 3. Практика — пересмотр задания 1 (`ASPA002_1`)

### Что было сломано до исправления

- `Program.cs` содержал остатки (`// переделать`, `типы`) — файл был нерабочим.
- `launchSettings.json` использовал порт `5000` вместо `5001` (конфликт с `ASPA002_2` и `ASPA002_3`).
- Порядок middleware был нарушен или отсутствовал.

### Что исправлено

- `Program.cs` переписан полностью с правильным порядком:
  1. `UseWelcomePage` (терминальный middleware для `/aspnetcore`);
  2. `UseDefaultFiles()` (перед `UseStaticFiles`);
  3. `UseStaticFiles()`.
- Порт исправлен на `5001` в `launchSettings.json` (и `https` на `5002`).
- `index.html`, `css/index.css`, `js/calc.js`, `jpg/Neumann.jpg` — проверены: все на месте, пути корректны.

### Проверенный результат

```
GET /aspnetcore  → 200 (страница ASP.NET Core WelcomePage)
GET /           → 200 (index.html через UseDefaultFiles + UseStaticFiles)
GET /index.html → 200
GET /css/index.css → 200
GET /js/calc.js    → 200
GET /jpg/Neumann.jpg → 200 (image/jpeg, 321230 байт)
```

---

## 4. Обобщённые выводы по всей работе

- **Middleware** — это не просто «функция в конвейере», это компонент с чётким контрактом (`RequestDelegate`, `Invoke`/`InvokeAsync`).
- **Порядок критичен**: ошибка в последовательности (`UseStaticFiles` до `UseDefaultFiles`) ломает стартовую страницу; `UseExceptionHandler` ниже endpoint'ов — исключения не перехватываются.
- **Endpoint (`MapGet`) ≠ Middleware (`Use*`)**: это разные уровни архитектуры (конвейер vs. конечная точка).
- **Обработка ошибок** работает только в режиме, отличном от `Development` (из-за авто-подключения `UseDeveloperExceptionPage`). Для проверки `UseExceptionHandler` нужен `Production` (`ASPA002_3` запускается с `ASPNETCORE_ENVIRONMENT=Production` в `launchSettings.json`).
