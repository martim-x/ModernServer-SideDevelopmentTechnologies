# Лабораторная работа № 2 — Ответы на вопросы

**Дисциплина:** Современные технологии серверной разработки
**Приложение:** ASPA (ASP.NET Core), проекты `ASPA002_1`, `ASPA002_2`, `ASPA002_3`
**Версия framework:** .NET 7.0 / 8.0 (всё сказанное ниже справедливо для обеих; поведение .NET 6+ одинаково)

---

## Как пользоваться документом

Документ отвечает на **все** вопросы лабораторной работы № 2:

- **часть 1** — вопросы задания 4 (пп. 15–22) плюс дополнительные вопросы преподавателя;
- **часть 2** — блок преподавателя про архитектуру ASP.NET Core: OWIN, Katana, конвейер обработки запросов, middleware, endpoint, «каким методом middleware передаёт дальше», схема обработки ошибок;
- **часть 3** — приложения: полный разбор кода `ASPA002_3`, пример собственного middleware, типичный порядок регистрации;
- **часть 4** — «Вопросы, которые задаст преподаватель сверх списка» — для подготовки к защите.

Каждый ответ построен одинаково: **определение → почему так → код (если уместно) → ссылка на документацию**.

> **Важно про терминологию.** В тексте задания есть опечатка `UseEcxeptionHandler`. Правильное имя — **`UseExceptionHandler`**. Ниже везде используется правильное написание.
> В тексте задания также встречается обрыв фразы «в каком месте ASPA применяется…» — ниже эти ответы дополнены и доведены до конца.

### Карта источников (что на чём основано)

| Утверждение | Где проверено |
|---|---|
| Определение middleware, терминальный middleware, short-circuiting, порядок | [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/) |
| Список встроенных middleware | [Built-in middleware (таблица в той же статье)](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/#built-in-middleware) |
| Требования к `Invoke` / `InvokeAsync`, `UseMiddleware<>`, время жизни | [Write custom middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write) |
| `UseWelcomePage` | [WelcomePageExtensions (API)](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.welcomepageextensions) |
| Статические файлы, `wwwroot`, `WebRootPath`, `UseDefaultFiles`, `UseFileServer`, `UseDirectoryBrowser` | [Serve static files](https://learn.microsoft.com/aspnet/core/fundamentals/static-files) |
| Web root = `{CONTENT ROOT}/wwwroot` | [ASP.NET Core fundamentals → Web root](https://learn.microsoft.com/aspnet/core/fundamentals/#web-root) |
| Обработка ошибок: `UseExceptionHandler`, `IExceptionHandlerPathFeature`, `UseStatusCodePages` | [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling) |
| Что именно делает `ExceptionHandlerMiddleware` (перезапуск конвейера, сброс endpoint, `Cache-Control: no-cache`) | исходный код `ExceptionHandlerMiddlewareImpl.cs` (dotnet/aspnetcore) |
| Endpoint: определение, 4 свойства, `UseRouting` / `UseEndpoints` | [Routing in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/routing) |
| `WebApplicationBuilder` / `CreateBuilder` | [WebApplication и WebApplicationBuilder](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/webapplication) |
| Какие middleware добавляет `WebApplication` сам | include `middleware8.md` в репозитории `dotnet/AspNetCore.Docs` (раздел «WebApplication») |
| OWIN, Katana, адаптеры, OWIN-ключи | [OWIN with ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/owin) |
| Katana: хостинг в IIS и самохостинг | [Getting started with OWIN and Katana](https://learn.microsoft.com/aspnet/aspnet/overview/owin-and-katana/getting-started-with-owin-and-katana) |
| Среда Development → `UseDeveloperExceptionPage` добавляется первым | [WebApplication и WebApplicationBuilder](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/webapplication) |
| Значения `LogLevel` | `LogLevel.cs` (dotnet/runtime) |
| Джон фон Нейман | Britannica, Wikipedia, первоисточник *First Draft of a Report on the EDVAC* (MIT/Smithsonian) |

---

# Часть 1. Вопросы задания 4 (пп. 15–22)

## 1. Паттерн Builder — назначение и применение в ASPA (п. 15)

### Краткий ответ

**Builder (построитель)** — порождающий паттерн, который разделяет **конструирование** сложного объекта и его **использование**: объект собирается пошагово, вызов за вызовом, а готовый результат получается на последнем шаге.

Зачем он нужен:

- конструирование объекта с большим числом необязательных параметров не превращается в конструктор с десятком перегруженных сигнатур;
- каждый шаг конфигурации — отдельная операция с собственным именем, читается как сценарий;
- один и тот же процесс сборки даёт разные экземпляры (конфигурации) объекта.

> Исправление опечатки в пересказе: «пошаговое создание сложных объекта» → «объектов» (родительный падеж множественного числа).

### Почему в ASPA это работает именно так

В ASP.NET Core объект-построитель — **`WebApplicationBuilder`**, а результат его сборки — **`WebApplication`** (приложение). Ровно это и есть пара «Builder → Product».

```csharp
var builder = WebApplication.CreateBuilder(args);   // шаг 0: создать построитель с настройками по умолчанию

builder.Services.AddHttpLogging(...);              // шаги конфигурации: DI, логирование, Kestrel, конфигурация

var app = builder.Build();                         // финальный шаг: «запечатать» и получить готовое приложение

app.UseHttpLogging();                              // здесь уже не Builder, а готовое приложение:
app.MapGet("/", () => "Hello World!");             // настраивается конвейер и конечные точки
app.Run();
```

Обратите внимание на разделение труда:

| Этап | Кто отвечает | Что происходит |
|---|---|---|
| Создание | `WebApplication.CreateBuilder(args)` | новый `WebApplicationBuilder` с предустановленными настройками |
| Конфигурация | `builder.Services`, `builder.Configuration`, `builder.Logging`, `builder.WebHost` | регистрация сервисов в DI, чтение `appsettings.json`, настройка Kestrel |
| Сборка | `builder.Build()` | конфигурация фиксируется, создаётся `WebApplication`; контейнер DI «запечатывается» |
| Применение | `app.Use*`, `app.Map*`, `app.Run()` | построение конвейера, регистрация endpoint, запуск сервера |

### Где ещё в ASPA встречается Builder

1. **`WebApplicationBuilder` / `CreateBuilder`** — основное место (п. 15 задания). Аналог для старого образца — `WebHost.CreateDefaultBuilder(args)`, актуальный для .NET Core 2.x–5.x.
2. **Конфигурация Kestrel** — тоже цепочка вызовов: `builder.WebHost.ConfigureKestrel(o => o.ListenLocalhost(5000));`
3. **Каскадные расширения типа `Add…`** — методы-расширения `IServiceCollection`, которые вызываются по цепочке и каждый добавляют свой сервис: `AddControllers().AddJsonOptions(...).AddRazorPages();` — тот же паттерн, но «продукт» здесь — конфигурация контейнера.
4. **`WebApplicationOptions`** — объект-параметр, через который задаётся среда, content root и web root: `WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = Environments.Staging })`.

### Код (минимальный, рабочий)

```csharp
var builder = WebApplication.CreateBuilder(args);

// --- шаги конфигурации ---
builder.Services.AddControllers();
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Information);

// --- финальный шаг: получить продукт ---
var app = builder.Build();

// --- использование продукта ---
app.UseStaticFiles();
app.MapGet("/", () => "Hello World!");
app.Run();
```

### Ссылки

- [WebApplication и WebApplicationBuilder в приложениях ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/webapplication) — `CreateBuilder` «initializes a new instance of the `WebApplicationBuilder` class with preconfigured defaults».
- [Миграция 5.0 → 6.0: новая модель размещения](https://learn.microsoft.com/aspnet/core/migration/50-to-60) — появление `WebApplicationBuilder`.
- API: [`WebApplication.CreateBuilder`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.webapplication.createbuilder).

---

## 2. Паттерн Chain of Responsibility — назначение и применение в ASPA (п. 16)

### Краткий ответ

**Chain of Responsibility (цепочка обязанностей)** — поведенческий паттерн, в котором запрос последовательно передаётся по цепочке обработчиков, и **каждый обработчик решает**: обработать запрос самому (и прервать цепочку) или передать его дальше. Запрос обрабатывает **первый** подходящий обработчик, а обработчик не обязан знать, что стоит за ним.

### Где в ASPA это конвейер middleware

**Конвейер обработки запросов (request pipeline / middleware pipeline) в ASP.NET Core — это и есть Chain of Responsibility.** Каждый middleware — обработчик цепочки:

- **до** вызова следующего middleware — «перед» (`before`): может изменить запрос, проверить условие, обогатить `HttpContext`, залогировать;
- **после** возврата управления — «после» (`after`): может изменить ответ, добавить заголовок, замерить время;
- **не вызвать следующий** — значит «обработать самостоятельно и остановить цепочку». Такой middleware называют **терминальным** (terminal middleware), а поведение — **short-circuiting** (короткое замыкание).

Каждый компонент решает по-своему:

- `UseStaticFiles` нашёл файл — ответил и **замкнул цепочку**, дальше не пошли;
- `UseExceptionHandler` — обернул вызов в `try/catch`, **передал дальше**, а при исключении перехватил его на обратном пути;
- `UseAuthentication` не нашёл у пользователя прав — **остановил цепочку** с кодом 401;
- `MapGet("/", …)` — терминальный обработчик, отдаёт строку и завершает конвейер.

Два важных отличия от «учебной» реализации GoF, которые стоит назвать, если спросят:

1. Цепочка **фиксирована один раз при старте приложения**, а не перебирается заново на каждый запрос. Она строится в `IApplicationBuilder.Build()` в связный список делегатов.
2. Обработчики не хранят ссылку на «следующего по условию» — следующий передаётся **внедрением в конструктор** как `RequestDelegate next`, а решение «передать дальше» выражается единственным вызовом `await next(context)`.

### Код: конвейер из трёх компонентов в терминах паттерна

```csharp
app.Use(async (context, next) =>          // Обработчик 1: «до» — пишет метку
{
    Console.WriteLine("--> вход в запрос " + context.Request.Path);
    await next(context);                  // передаёт запрос следующему обработчику
    Console.WriteLine("<-- выход из запрос " + context.Request.Path);   // «после»
});

app.UseStaticFiles();                     // Обработчик 2: если файл найден — сам отвечает и замыкает цепочку

app.MapGet("/", () => "Hello World!");    // Обработчик 3 (терминальный): всегда завершает конвейер
```

### Схема «луковицы» (классическая иллюстрация поведения)

```
                     ЗАПРОС
                       |
                       v
        +-------------------------------+
        |  Middleware 1  (Use …)       |   "до":  Console.WriteLine("-->")
        |    next(context) -------------+--> (передаёт дальше)
        |  Middleware 2  (UseStaticFiles)|
        |    next(context) -------------+--> (передаёт дальше, если файла нет)
        |  Endpoint     (MapGet)        |   терминальный обработчик: пишет ответ
        +-------------------------------+
                       |
                       |  ОТВЕТ (возврат по цепочке в обратном порядке)
                       v
        Middleware 2  "после"
                       |
                       v
        Middleware 1  "после":  Console.WriteLine("<--")
                       |
                       v
                     КЛИЕНТ
```

### Ссылки

- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/) — «Each middleware: chooses whether to pass the request to the next middleware; can perform work before and after the next middleware», а также описание short-circuiting и terminal middleware.
- [Write custom middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write) — «Chain multiple request delegates together with `Use`… The `next` parameter represents the next delegate in the pipeline. You can short-circuit the pipeline by *not* calling the `next` parameter.»

---

## 3. Понятие Middleware (п. 17)

### Определение

**Middleware (промежуточное ПО) — программный компонент, который включается в конвейер (pipeline) обработки HTTP-запросов и ответов.** Каждый middleware-компонент:

1. получает запрос;
2. решает, передавать ли его следующему компоненту цепочки;
3. может выполнить работу **до** и **после** вызова следующего компонента.

Формально — это делегат, который оборачивает следующий обработчик и возвращает новый:

```csharp
// RequestDelegate — «обработчик запроса»
public delegate Task RequestDelegate(HttpContext context);
```

`ApplicationBuilder.Use(Func<RequestDelegate, RequestDelegate>)` — метод, который «adds the middleware to the application request pipeline»: на вход принимается функция «дать следующий обработчик → получить обёртку», на выход — обёртка. Так и строится цепочка.

### Требования к классу middleware (проверено по документации)

Если middleware оформлен классом, то:

- **публичный конструктор** с параметром типа `RequestDelegate` (это «следующий» обработчик);
- **публичный метод `Invoke` или `InvokeAsync`**, который:
  - возвращает `Task`;
  - принимает первым параметром `HttpContext`;
- дополнительные параметры конструктора и `Invoke`/`InvokeAsync` заполняются **внедрением зависимостей (DI)**.

Важная деталь про время жизни: middleware создаётся **один раз на всё время жизни приложения**. Поэтому сервисы с областью видимости `scoped`, нужные **на каждый запрос**, передают не в конструктор, а в параметры `InvokeAsync` — их тогда создаёт контейнер для конкретного запроса.

### Что даёт middleware

- единая «труба» для сквозной функциональности (логирование, аутентификация, ошибки, кэш, статика) — не нужно дублировать её в каждом обработчике;
- **порядок регистрации = порядок выполнения** (это критично: обработка ошибок ставят первыми, статические файлы — рано, `UseRouting` — перед endpoint);
- возможность разрывать цепочку (short-circuit) для экономии работы.

### Реальный минимальный пример (по требованиям документации)

```csharp
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    // Публичный конструктор с параметром RequestDelegate — обязательное требование.
    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Публичный метод Invoke: возвращает Task, первый параметр — HttpContext.
    public async Task Invoke(HttpContext context)
    {
        Console.WriteLine($"Запрос: {context.Request.Method} {context.Request.Path}");
        await _next(context);                       // <-- ПЕРЕДАЧА ДАЛЬШЕ
    }
}
```

Подключение — расширением для `IApplicationBuilder`, чтобы в `Program.cs` писать `app.UseRequestLogging()`:

```csharp
public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestLoggingMiddleware>();
    }
}
```

Схема «свёртки» из одного метода `Use` (тоже официально поддерживаемый способ):

```csharp
app.Use(async (context, next) =>
{
    Console.WriteLine($"--> {context.Request.Path}");
    await next(context);
    Console.WriteLine($"<-- {context.Request.Path}");
});
```

### Ссылки

- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/) — определение, терминальный middleware, ветвление (`Map`, `MapWhen`).
- [Write custom middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write) — точный перечень требований к классу; время жизни; `UseMiddleware<>`; два перегруженных `Use` (рекомендуется перегрузка с `RequestDelegate` — «saves two internal per-request allocations»).
- [Factory-based middleware activation](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/extensibility) — альтернатива: `IMiddleware` (сильная типизация, активация на каждый запрос).

---

## 4. Понятие «статический файл» (п. 18)

### Определение

**Статический файл (static file, static web asset)** — файл, содержимое которого **не формируется приложением во время запроса**: приложение не генерирует его динамически, а отдаёт клиенту как есть, из памяти или с диска. Примеры: `*.html`, `*.css`, `*.js`, `*.jpg`, `*.png`, `*.svg`, `*.ico`, шрифты `*.woff2`.

Документация формулирует так: статические файлы «aren't dynamically generated and are served directly to clients, including HTML, CSS, images, and JavaScript».

Признаки, по которым файл считается статическим:

| Признак | Пояснение |
|---|---|
| Нет динамической генерации | Содержимое не зависит от запроса, сессии, БД |
| Обслуживается middleware | `StaticFileMiddleware` / `UseStaticFiles()` — встроенный компонент, **не** endpoint приложения |
| Терминальный компонент | Найден файл → ответ отправлен, цепочка **разорвана** (short-circuit) |
| Служебные заголовки | Проставляются `ETag`, `Last-Modified`, `Content-Type` — для кэширования и корректного типа содержимого |
| Хранится в web root | По умолчанию `{CONTENT ROOT}/wwwroot` и подкаталоги |

### Почему их отдаёт отдельный middleware, а не код приложения

- **Экономия работы**: файл отдан — дальнейшие компоненты конвейера (маршрутизация, авторизация, контроллеры) выполняться не будут.
- **Сквозная забота**: заголовки кэширования, тип содержимого, обработка `Range`-запросов делаются один раз в инфраструктуре, а не в каждом обработчике.
- **Безопасность**: `wwwroot` — единственная папка, содержимое которой по умолчанию доступно извне; всё, что не должно публиковаться (`.cs`, `.json` с секретами), держится вне web root.

### Код

```csharp
var app = builder.Build();

app.UseStaticFiles();      // файлы из web root (по умолчанию wwwroot) — короткое замыкание при совпадении

app.MapGet("/", () => "Hello World!");
app.Run();
```

Дополнительный каталог вне `wwwroot` (для задания 2, п. 9 — каталог `Picture`):

```csharp
using Microsoft.Extensions.FileProviders;

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Picture")),  // откуда физически читать
    RequestPath = "/static"                                            // по какому URL отдавать
});
// теперь:  GET /static/Neumann.jpg  ->  {ContentRoot}/Picture/Neumann.jpg
```

### Ссылки

- [Serve static files in ASP.NET Core apps](https://learn.microsoft.com/aspnet/core/fundamentals/static-files) — определение статических файлов, обслуживание вне web root через `PhysicalFileProvider`, `UseFileServer`, `UseDirectoryBrowser`.
- [ASP.NET Core fundamentals → Web root](https://learn.microsoft.com/aspnet/core/fundamentals/#web-root) — «By default, static files are only served from the web root directory and its sub-directories».

---

## 5. Отличительный признак наименования подключаемого Middleware-компонента (п. 19)

### Краткий ответ

**Общепринятый (соглашённый) признак — префикс `Use` в имени метода расширения**, которым middleware подключается к конвейеру.

```csharp
app.UseStaticFiles();
app.UseDefaultFiles();
app.UseExceptionHandler("/Error");
app.UseDeveloperExceptionPage();
app.UseHttpLogging();
```

Это не случайность, а часть **соглашения по именованию**, которое Microsoft зафиксировала в документации: конвейер настраивается методами расширения `Run`, `Map` и `Use` из пространства имён `Microsoft.AspNetCore.Builder`.

### Полная карта соглашения

| Префикс / форма | Назначение | Пример |
|---|---|---|
| `Use<ЧтоТо>()` | подключить middleware, который **может передать** запрос дальше | `UseStaticFiles`, `UseRouting`, `UseAuthentication` |
| `Run(...)` | подключить **терминальный** делегат (уже не передаёт дальше) | `app.Run(async ctx => …)` |
| `Map<Метод>(…)` / `MapGet` / `MapPost` / `MapControllers` / `MapRazorPages` | зарегистрировать **endpoint** — конечную точку, а не middleware | `app.MapGet("/", …)` |
| `Add<ЧтоТо>()` | зарегистрировать **сервис в DI**, а не компонент конвейера | `builder.Services.AddHttpLogging()` |
| Класс `*Middleware` | имя класса самого компонента | `StaticFileMiddleware`, `ExceptionHandlerMiddleware` |
| Класс `*Extensions` | имя класса с методами расширения для подключения | `StaticFileExtensions`, `WelcomePageExtensions` |

Различать `Use…` и `Add…` важно: `Use…` — про **конвейер обработки запроса**, `Add…` — про **регистрацию сервисов в контейнере DI**. Оба используют `builder`/`app`, но в разных ролях (`builder.Services.Add…` против `app.Use…`).

### Ссылки

- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/) — «Configure request delegates by using the `Run`, `Map`, and `Use` extensions methods»; таблица встроенных middleware даёт полный список имён с префиксом `Use`.
- [Write custom middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write) — «Typically, an extension method is created to expose the middleware through `IApplicationBuilder`».

---

## 6. Встроенные Middleware-компоненты (п. 20)

Ниже — реальные встроенные компоненты из документации ASP.NET Core с назначением каждого. Сгруппированы по назначению.

### Обработка исключений и ошибок

| Метод | Назначение |
|---|---|
| `UseDeveloperExceptionPage()` | Страница с подробной информацией об исключении (стек-трейс, query string, cookie, заголовки, endpoint metadata). **Только для среды `Development`.** |
| `UseExceptionHandler(path)` / `UseExceptionHandler(lambda)` / `UseExceptionHandler(options)` | Ловит необработанные исключения, логирует их и **перезапускает конвейер** по указанному пути (`/Error`) либо по заданному делегату. Если ответ уже начат отправляться — перезапуск не происходит. |
| `UseDatabaseErrorPage()` | Страница ошибок, связанных с базой данных (только `Development`). |
| `UseStatusCodePages()` | Для ответов 400–599 **без тела** выводит текстовую заглушку («Status Code: 404; Not Found»). Исключения **не** ловит. |
| `UseStatusCodePagesWithRedirects(urlTemplate)` | Отправляет 302 и перенаправляет клиента на страницу ошибки (в адресной строке браузера будет адрес обработчика, код 200). |
| `UseStatusCodePagesWithReExecute(urlTemplate)` | Формирует тело, **перезапуская конвейер** по другому пути; исходный код состояния сохраняется, адресная строка браузера не меняется. |

### Принудительный HTTPS и безопасность

| Метод | Назначение |
|---|---|
| `UseHttpsRedirection()` | Перенаправляет все HTTP-запросы на HTTPS. |
| `UseHsts()` | Добавляет заголовок `Strict-Transport-Security` — браузер запомнит, что на этот домен надо ходить только по HTTPS. |
| `UseForwardedHeaders()` | Прокидывает заголовки, полученные от обратного прокси (схема, хост, IP клиента, метод). |

### Маршрутизация и конечные точки

| Метод | Назначение |
|---|---|
| `UseRouting()` | Добавляет сопоставление маршрутов: по URL и методу HTTP выбирается endpoint. |
| `UseEndpoints(…)` | Выполняет делегат выбранного endpoint. При совпадении — **терминальный** компонент. |
| `UseHttpMethodOverride()` | Позволяет входящему POST-запросу переопределить метод — заголовком `X-HTTP-Method-Override` (по умолчанию) или значением формы. Нужно клиентам, которые умеют только GET и POST. |

### Аутентификация, авторизация, защита от подделок, cookie

| Метод | Назначение |
|---|---|
| `UseAuthentication()` | Аутентификация пользователя **до** того, как потребуется `HttpContext.User`. Терминальный для OAuth-callback. |
| `UseAuthorization()` | Проверка прав доступа; ставится сразу после `UseAuthentication`, чтобы видел выбранный endpoint. |
| `UseAntiforgery()` | Защита от подделки запросов (CSRF). Ставится **после** `UseAuthentication` и `UseAuthorization`, до endpoint. |
| `UseCookiePolicy()` | Согласие пользователя на cookie и минимальные требования к ним (`secure`, `SameSite`) — GDPR. |
| `UseCors()` | Cross-Origin Resource Sharing. Должен идти **до** `UseResponseCaching`, `UseAuthentication`, `UseAuthorization`. |

### Статические файлы и файловый сервер

| Метод | Назначение |
|---|---|
| `UseStaticFiles()` | Отдаёт статические файлы из web root (`wwwroot`). Терминальный при совпадении. |
| `UseDefaultFiles()` | Переписывает URL запроса на имя файла по умолчанию, если такой файл есть. **Сам файл не отдаёт** — поэтому вызывается **до** `UseStaticFiles()`. Стандартный список: `default.htm`, `default.html`, `index.htm`, `index.html`. |
| `UseFileServer()` | Объединяет `UseStaticFiles` + `UseDefaultFiles` и, опционально, `UseDirectoryBrowser`. |
| `UseDirectoryBrowser()` | Показывает листинг каталога со ссылками. **По умолчанию выключен** (из соображений безопасности); требует `builder.Services.AddDirectoryBrowser()`. |
| `UseSpaStaticFiles()` / `UseSpa()` | Обслуживание статики и отдача index.html одностраничного приложения (SPA). |
| `MapStaticAssets()` | Вариант .NET 9+ на endpoint-маршрутизации: сборка, сжатие и «отпечатки» (fingerprinting) на этапе сборки. В лабораторной (.NET 7/8) не используется. |

### Логирование, кэширование, сжатие

| Метод | Назначение |
|---|---|
| `UseHttpLogging()` | Логирование HTTP-запросов и ответов. Ставится в начале конвейера. |
| `UseResponseCompression()` | Сжатие ответов (gzip / Brotli). |
| `UseResponseCaching()` | Кэширование ответов на стороне сервера с участием клиента. |
| `UseOutputCache()` | Кэширование ответов под полным контролем сервера (.NET 7+). |
| `UseRateLimiter()` | Ограничение частоты запросов (по политикам, в т. ч. ограничение по конечной точке). Доступно начиная с .NET 7. |
| `UseW3CLogging()` | Пишет файлы журналов доступа в формате W3C Extended Log File Format. |

### Сессии, локализация, прочее

| Метод | Назначение |
|---|---|
| `UseSession()` | Управление состоянием сессии (Razor Pages / MVC). Ставится после `UseCookiePolicy` и до MVC. |
| `UseRequestLocalization()` | Локализация запроса. Ставится до middleware, чувствительных к языку. |
| `UseRequestDecompression()` | Распаковка сжатого тела запроса — до чтения тела запроса. |
| `UseRequestTimeouts()` | Таймауты запроса, глобальные и для отдельных endpoint (.NET 8+). Ставится после `UseExceptionHandler`, `UseDeveloperExceptionPage`, `UseRouting`. |
| `UseRewriter()` | Переписывание и перенаправление URL по правилам. |
| `UseHealthChecks()` | Ответы о состоянии приложения и его зависимостей. |
| `UseWebSockets()` | Поддержка протокола WebSockets. |
| `UseOwin(…)` | Запуск OWIN-middleware в конвейере ASP.NET Core (пакет `Microsoft.AspNetCore.Owin`). |
| `UseMiddleware<T>()` | Подключение **собственного** типизированного middleware. |
| `UseWhen(predicate, branch)` | Условное ветвление конвейера. |
| `UseWelcomePage(…)` | Приветственная (стартовая) страница для нового приложения. Реализуется `WelcomePageMiddleware`. |

### Три компонента, которые чаще всего забывают назвать в лабораторной

1. **`UseDefaultFiles()`** — именно он делает `Index.html` стартовой страницей (задание 1, п. 5; задание 2, п. 9 — с заменой списка имён).
2. **`UseWelcomePage(…)`** — приветственная страница ASP.NET Core (задание 1, п. 2).
3. **`UseFileServer()`** — комбинация статики + стартовой страницы, часто используется вместо двух отдельных вызовов.

### Ссылки

- [Built-in middleware — таблица в статье о middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/#built-in-middleware) (описание, для какого стека UI, требования к порядку).
- [Serve static files](https://learn.microsoft.com/aspnet/core/fundamentals/static-files) — `UseDefaultFiles`, `UseFileServer`, `UseDirectoryBrowser`.
- [Handle errors in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling) — `UseStatusCodePages` и варианты.
- [`WelcomePageExtensions`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.welcomepageextensions) — «Adds the `WelcomePageMiddleware` to the pipeline with the given path».

---

## 7. Директория для статических файлов по умолчанию (п. 21)

### Краткий ответ

**`wwwroot`.**

Более точно: по умолчанию web root — это `{CONTENT ROOT}/wwwroot`, то есть каталог `wwwroot` в корне проекта (content root). Именно оттуда `UseStaticFiles()` берёт файлы; всё остальное содержимое проекта клиенту не доступно.

### Что такое content root и web root

| Понятие | Что это | По умолчанию |
|---|---|---|
| **Content root** | Базовая папка для файлов приложения: `.dll`, `appsettings.json`, шаблоны Razor, сам web root | Каталог проекта (при `WebApplication.CreateBuilder` content root = текущий каталог) |
| **Web root** | Базовая папка для публичных статических ресурсов: CSS, JS, изображения | `{CONTENT ROOT}/wwwroot` |

### Как это устроено технически

Путь хранится не «в коде», а в объекте окружения **`IWebHostEnvironment`**:

| Свойство | Что хранит |
|---|---|
| `WebRootPath` | Путь к web root в виде строки (по умолчанию — путь к `wwwroot`) |
| `WebRootFileProvider` | Поставщик файловой системы для web root (`IFileProvider`) |
| `ContentRootPath` | Путь к content root |

`UseStaticFiles()` без параметров работает именно с ними: «Files are served from the path specified in `IWebHostEnvironment.WebRootPath` or `IWebHostEnvironment.WebRootFileProvider`, which defaults to the web root folder, typically `wwwroot`».

В файлах `.cshtml`/Razor путь `~/` указывает на web root — такой путь называется **виртуальным**.

### Как это меняется (три способа)

**1. Сменить сам web root целиком — `UseWebRoot`:**

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseWebRoot("wwwroot-custom");   // теперь web root — другая папка
var app = builder.Build();
app.UseStaticFiles();
```

**2. Задать путь через `WebApplicationOptions`:**

```csharp
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    WebRootPath = "wwwroot-custom"
});
```

> Тонкость из документации: если в среде `Development` в `wwwroot` и в новом web root лежат одноимённые файлы, отдаётся версия из `wwwroot`; в остальных средах — из `WebRootPath`. Чтобы этого не было, дубликаты удаляют либо выключают static web assets.

**3. Не менять web root, а добавить ещё один источник — `UseStaticFiles` + `PhysicalFileProvider`:**

```csharp
using Microsoft.Extensions.FileProviders;

var app = builder.Build();

app.UseStaticFiles();                                  // обычный wwwroot

app.UseStaticFiles(new StaticFileOptions               // дополнительный каталог Picture
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Picture")),
    RequestPath = "/static"
});
```

Именно третий способ нужен для задания 2, п. 9: «кроме стандартного (`wwwroot`) директория сделайте ещё один дополнительный директорий с именем `Picture`». Физически файлы лежат вне web root, но по URL `/static/Neumann.jpg` отдаются как статические — при этом в `Neumann.html` ссылку надо поменять на `static/Neumann.jpg`.

Ещё вариант — **составной поставщик** `CompositeFileProvider`, чтобы дополнительная папка считалась частью web root:

```csharp
var webRootProvider  = new PhysicalFileProvider(builder.Environment.WebRootPath);
var pictureProvider  = new PhysicalFileProvider(
    Path.Combine(builder.Environment.ContentRootPath, "Picture"));
app.Environment.WebRootFileProvider =
    new CompositeFileProvider(webRootProvider, pictureProvider);
```

### Ссылки

- [Serve static files → «Change the web root directory»](https://learn.microsoft.com/aspnet/core/fundamentals/static-files) — `UseWebRoot`, `WebRootPath`, `PhysicalFileProvider`, `CompositeFileProvider`.
- [ASP.NET Core fundamentals → Web root / Content root](https://learn.microsoft.com/aspnet/core/fundamentals/) — определения и значение `{CONTENT ROOT}/wwwroot`.

---

## 8. Джон фон Нейман (п. 22)

### Кратко (то, что нужно на защите)

**Джон фон Нейман (János Neumann, 28 декабря 1903, Будапешт — 8 февраля 1957, Вашингтон)** — венгеро-американский математик, один из крупнейших учёных XX века. Для курса «Современные технологии серверной разработки» важен **не** его вклад в математику, а то, что он один из создателей **архитектуры фон Неймана** — основы практически всех современных вычислительных машин, включая серверы, на которых работают приложения ASP.NET Core.

### Биографическая справка

| Параметр | Сведения |
|---|---|
| Имя при рождении | **János Lajos Neumann** (венгер. *Neumann János Lajos*); «фон Нейман» — аристократическая немецкая частица, добавленная при англоязычной адаптации фамилии |
| Даты жизни | 28.12.1903 — 08.02.1957 (53 года) |
| Место рождения / смерти | Будапешт, Венгрия / Вашингтон, США |
| Образование | Университет Пазмань Петера (Будапешт), Берлинский университет, ETH Zürich |
| Гражданство | Венгрия, с 1937 — США |
| Ключевые области | математическая логика, теория множеств, функциональный анализ, теория операторов, математическая физика, теория игр, эргодическая теория, теория автоматов, вычислительная математика |

### Основные достижения

- **Теория игр** — «теория парторных игр» (*Theory of Parlor Games*, 1928), положившая начало целой науке.
- **Математическая формулировка квантовой механики** (1926) — операторный подход.
- **Алгебра операторов / фон-Неймановские алгебры** — фундамент функционального анализа.
- **Манхэттенский проект**: во время Второй мировой войны работал над математическими моделями взрывных линз для implosion-типа ядерного оружия; внёс вклад в гидродинамику (решение о сильных ударных волнах, ныне «Тейлор — фон Нейман — Седов»).
- **Член Комиссии по атомной энергии США** (1955–1957).
- С 1933 года — профессор **Института перспективных исследований (Institute for Advanced Study)** в Принстоне.

### Связь с темой лабораторной

**1945 год — «First Draft of a Report on the EDVAC»** («Первый вариант отчёта об EDVAC»), документ на 101 странице, подготовленный по контракту с Армейским орденацством США и Университетом Пенсильвании (Школа Moore), датирован **30 июня 1945 года**. Это **первое опубликованное описание логического устройства компьютера с хранимой программой** — то, что стало называться **архитектурой фон Неймана**.

Ключевые идеи, прямо лёгшие в основу всей вычислительной техники:

1. **Единая память** содержит и данные, и команды программы (т.н. «программа в памяти»).
2. **Команды закодированы** в том же формате, что и данные, в виде чисел, поэтому сама программа может изменять саму себя.
3. Последовательное выполнение команд под управлением счётчика программы, с возможностью перехода.

Отсюда же — классический «** bottlenек фон Неймана**»: поскольку процессор и память обмениваются данными по одной шине, их скорости упираются в пропускную способность этой шины; разделение на кэши (в том числе L1/L2/L3 процессора) — основной способ обойти это ограничение.

> Историческая справедливость: сам фон Нейман в документе не назвал соавторов архитектуры — Джона Моучли и Преспера Эккерта, чьи идеи лежали в основе, — что породило долгую дискуссию о приоритете. Этот факт стоит упомянуть, если преподаватель спросит «а что думают историки».

Почему это в лабораторной: в задании 1 и 2 требуется сверстать `Index.html` / `Neumann.html` с портретом `Neumann.jpg` — то есть тема персоны является темой самой работы, а не случайным наполнением.

### Ссылки

- [John von Neumann — Britannica](https://www.britannica.com/biography/John-von-Neumann)
- [John von Neumann — Wikipedia](https://en.wikipedia.org/wiki/John_von_Neumann)
- [First Draft of a Report on the EDVAC — Wikipedia](https://en.wikipedia.org/wiki/First_Draft_of_a_Report_on_the_EDVAC) (дата 30.06.1945, 101 страница, первое описание концепции хранимой программы)
- Первоисточник: [First draft of a report on the EDVAC (MIT)](https://people.csail.mit.edu/brooks/idocs/firstdraft.pdf), копия — [Smithsonian Digital Library](https://library.si.edu/digital-library/book/firstdraftofrepo00vonn)

---

# Часть 2. Дополнительные вопросы преподавателя: архитектура ASP.NET Core

## 9. Архитектура ASP.NET Core

### Слои архитектуры

ASP.NET Core состоит из трёх крупных слоёв. Каждый следующий «лежит на» предыдущем, и каждый можно заменить:

```
+------------------------------------------------------------------+
|  Слой 3: ПРИЛОЖЕНИЕ (endpoint)                                    |
|  Конечные точки: MapGet / MapPost / MapRazorPages / MapControllers |
|  Содержимое: обработчики запросов, бизнес-логика                   |
+------------------------------------------------------------------+
|  Слой 2: КОНВЕЙЕР ОБРАБОТКИ ЗАПРОСОВ (middleware pipeline)        |
|  ExceptionHandler, StaticFiles, Routing, Auth, CookiePolicy …      |
|  Задача: сквозная функциональность, порядок, разрыв цепочки        |
+------------------------------------------------------------------+
|  Слой 1: ХОСТИНГ + HTTP-СЕРВЕР                                     |
|  Хост: WebApplicationBuilder / WebApplication / IHost             |
|  Сервер: Kestrel (по умолчанию), HTTP.sys, IIS HTTP Server         |
|  Задача: принять TCP-соединение, сформировать HttpContext,          |
|          отдать ответ                                               |
+------------------------------------------------------------------+
```

| Слой | Ключевые понятия | Что можно заменить |
|---|---|---|
| Хостинг/сервер | `WebApplicationBuilder`, `WebApplication`, Kestrel, модель размещения (in-process / out-of-process), reverse proxy | Другой HTTP-сервер: HTTP.sys, IIS HTTP Server, или поставить за Nginx/IIS/Apache |
| Конвейер | `RequestDelegate`, `IApplicationBuilder`, middleware | Порядок и набор middleware; добавить свой компонент |
| Приложение | `IEndpointRouteBuilder`, `RouteEndpoint`, `RequestDelegate`, `HttpContext`, `IResult` | Модель программирования: minimal API, MVC, Razor Pages, Blazor |

### Про сервер и хостинг (что стоит сказать, если спросят)

- ASP.NET Core-приложение работает **с внутрипроцессным HTTP-сервером**: сервер слушает HTTP-запросы и «поднимает» их до приложения как набор **request features**, собранных в **`HttpContext`**.
- **Kestrel** — сервер по умолчанию, кроссплатформенный. Он же может работать как пограничный сервер за обратным прокси.
- **HTTP.sys** — только Windows, поверх драйвера ядра; даёт разделяемые порты, кэширование ответов, Windows-аутентификацию в режиме ядра.
- **Модели размещения**: in-process (приложение в том же процессе, что и рабочий процесс IIS) и out-of-process (Kestrel за модулем ASP.NET Core Module / прокси).

### Схема полного пути запроса

```
Браузен
  │  TCP-соединение, HTTP-запрос:  GET /test2 HTTP/1.1
  ▼
┌────────────────────────────────────────────────────────────┐
│ Kestrel                                                    │
│  принимает сокет, читает HTTP, формирует request features  │
│  и собирает HttpContext { Request, Response, Features,     │
│  User, RequestServices, GetEndpoint() ... }                │
└────────────────────────────────────────────────────────────┘
  │  RequestDelegate next(context) — передача дальше по цепочке
  ▼
┌────────────────────────────────────────────────────────────┐
│ Конвейер middleware                                        │
│  [1] UseExceptionHandler("/error")   try/catch             │
│  [2] UseHttpLogging()                                      │
│  [3] UseStaticFiles()             (разрыв при совпадении)  │
│  [4] UseRouting()                  (выбор endpoint)        │
│  [5] UseEndpoints()                (выполнение endpoint)   │
└────────────────────────────────────────────────────────────┘
  │  endpoint MapGet("/test1", …) → исключение
  ▼
Ответ (или перехват исключения — см. раздел 14)
```

### Ссылки

- [Web server implementations in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/servers/) — Kestrel, HTTP.sys, IIS HTTP Server, модели размещения.
- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/).
- [Routing in ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/routing/).

---

## 10. OWIN: что это и какие есть реализации (например, Katana)

### Что такое OWIN

**OWIN = Open Web Interface for .NET** — это **стандарт (спецификация) интерфейса между веб-сервером и веб-приложением** в экосистеме .NET. Это именно спецификация, а не реализация: она описывает, как middleware-компоненты собираются в конвейер и как через него проходят запрос и ответ.

Ключевая идея — **разделение (decoupling)**: приложение перестаёт зависеть от конкретного сервера. Появляется тонкий слой абстракции, поверх которого серверы и приложения взаимозаменяемы.

Как это устроено технически:

- весь обмен идёт через **`IDictionary<string, object>`** — «OWIN environment»;
- обработчик — это **`Func<IDictionary<string, object>, Task>`** (в спецификации — *AppFunc*);
- конкретные данные лежат под ключами вида `owin.RequestBody`, `owin.ResponseHeaders`, `owin.RequestMethod`, `owin.ResponseStatusCode`, `ssl.ClientCertificate`, `server.RemoteIpAddress` и т. д. Полный список — в документации.

Цели, из которых всё выросло:

1. веб-приложение не зависит от конкретного сервера (можно self-host в любом процессе — хоть в службе Windows);
2. middleware можно писать один раз и переносить между хостами;
3. контракт между компонентами сведён к одному делегату — отсюда модульность и скорость внедрения новизн.

### Реализации: Katana

**Katana** — проект Microsoft, **набор открытых компонентов с реализацией OWIN**. Это эталонная реализация спецификации, а не сама спецификация.

Компоненты Katana, которые стоит назвать:

| Пакет / инструмент | Что делает |
|---|---|
| `Microsoft.Owin.Host.SystemWeb` | Хостинг OWIN-приложения в **IIS** — конвейер OWIN работает внутри конвейера ASP.NET (`System.Web`) |
| `Microsoft.Owin.Host.HttpListener` | **Self-hosting**: использует `System.Net.HttpListener` как HTTP-сервер, приложение само создаёт процесс |
| `OwinHost.exe` | Утилита self-hosting: поднимает OWIN-приложение, по умолчанию на порту 5000 (`-p` — другой порт, `-s` — другая серверная сборка) |
| `Microsoft.Owin.SelfHost` | Пакет для self-hosting, включающий хост на `HttpListener` и `WebApp.Start<TStartup>(url)` |
| `Microsoft.Owin.Testing` | Тестовый хост (в памяти) для OWIN-приложений |

Точка входа OWIN-приложения — класс с методом `Configuration(IAppBuilder app)`, который и собирает конвейер.

> Существенный факт: **`Microsoft.Owin.*` (Katana) недоступны для .NET Core**, потому что у ASP.NET Core есть эквивалентные замены. Новые проекты на Katana не строят — Microsoft прямо рекомендует ASP.NET Core.

### Как OWIN соотносится с ASP.NET Core

ASP.NET Core **поддерживает OWIN**, но не на уровне ядра, а через пакет **`Microsoft.AspNetCore.Owin`**, который даёт **два адаптера**:

- ASP.NET Core → OWIN (OWIN-приложение поверх ASP.NET Core);
- OWIN → ASP.NET Core (ASP.NET Core поверх OWIN-совместимого сервера/хоста).

OWIN-middleware подключается в конвейере ASP.NET Core через `UseOwin(...)` или `UseMiddleware<OwinMiddleware>(...)`:

```csharp
using Microsoft.AspNetCore.Owin;   // пакет Microsoft.AspNetCore.Owin

app.UseOwin(pipeline =>
{
    pipeline(next => async environment =>
    {
        // что-то ДО
        await next(environment);
        // что-то ПОСЛЕ
    });
});
```

Сам пример OWIN-middleware по документации:

```csharp
public Task OwinHello(IDictionary<string, object> environment)
{
    string responseText = "Hello World via OWIN";
    byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);

    var responseStream = (Stream)environment["owin.ResponseBody"];
    var responseHeaders = (IDictionary<string, string[]>)environment["owin.ResponseHeaders"];

    responseHeaders["Content-Length"] = new string[] { responseBytes.Length.ToString(CultureInfo.InvariantCulture) };
    responseHeaders["Content-Type"]    = new string[] { "text/plain" };

    return responseStream.WriteAsync(responseBytes, 0, responseBytes.Length);
}
```

Практические замечания из документации:

- использовать адаптеры дороже по производительности; приложению, состоящему только из компонентов ASP.NET Core, пакет `Microsoft.AspNetCore.Owin` не нужен;
- заголовки ответа нужно менять **до** первой записи в поток ответа;
- несколько вызовов `UseOwin` не рекомендуются — OWIN-компоненты лучше группировать вместе.

Связь с конвейером ASP.NET Core (ответ преподавателю, если спросят «а зачем нам OWIN, если есть pipeline?»): идея **та же** — цепочка обработчиков, каждый решает, передать дальше или ответить самому. Разница в контракте: OWIN оперирует словарём environment и делегатом `Func<IDictionary<string,object>, Task>`, ASP.NET Core — объектом `HttpContext` и типом `RequestDelegate`.

### Ссылки

- [OWIN with ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/owin) — спецификация, адаптеры `Microsoft.AspNetCore.Owin`, `UseOwin`, полный список OWIN-ключей, пример OWIN-middleware.
- [Getting started with OWIN and Katana](https://learn.microsoft.com/aspnet/aspnet/overview/owin-and-katana/getting-started-with-owin-and-katana) — «OWIN is a community-owned specification, not an implementation. The Katana project is a set of open-source OWIN components developed by Microsoft»; хостинг в IIS и self-hosting.
- [OWIN and Katana — обзор](https://learn.microsoft.com/aspnet/aspnet/overview/owin-and-katana/) — «Katana is a flexible set of components for building and hosting OWIN-based web apps. New development should use ASP.NET Core. The `Microsoft.Owin.*` libraries are not available for .NET Core».
- Спецификация: [owin.org](http://owin.org/spec/spec/owin-1.0.0.html)

---

## 11. Конвейер обработки запросов (middleware pipeline)

### Определение

**Конвейер обработки запросов (request pipeline, middleware pipeline)** — упорядоченная последовательность компонентов (middleware), собранная один раз при старте приложения, через которую проходит **каждый** HTTP-запрос. Конвейер состоит из двух встречных потоков:

- **прямой (request)** — запрос передаётся от компонента к компоненту, пока не будет обработан или цепочка не разорвётся;
- **обратный (response)** — управление возвращается по той же цепочке в обратном порядке, и каждый компонент может дописать в ответ (заголовок, замер времени, модификация тела).

Цитата из документации: «After the third middleware processes the request, the request passes back through the prior two middlewares in reverse order for additional processing after their `next()` statements before leaving the app as a response to the client.»

### Ключевые свойства

| Свойство | Смысл |
|---|---|
| Порядок регистрации = порядок выполнения | `app.UseA(); app.UseB();` → A раньше B и на входе, и на выходе |
| Строится один раз | `IApplicationBuilder.Build()` превращает цепочку делегатов в `RequestDelegate` |
| Передача — вызовом `next` | Либо `await next(context)`, либо `return _next(context)` |
| Разрыв цепочки возможен | Не вызвать `next` = обработать запрос самостоятельно (terminal middleware) |
| Ветвление возможно | `Map` (по префиксу пути), `MapWhen` (по предикату), `UseWhen` (условно) |
| Порядок критичен | «The order is **critical** for security, performance, and functionality» |

### Полная ASCII-схема конвейера (направление запроса и обратный путь ответа)

```
                        ЗАПРОС (request)  ──►
  ┌────────────────────────────────────────────────────────────────────────┐
  │ 1. ExceptionHandler     try { await next(ctx) } catch { … }            │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 2. HSTS                 заголовок Strict-Transport-Security            │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 3. HttpLogging          лог «начало запроса»                           │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 4. StaticFiles          нашёл файл? ──► ОТВЕТ и РАЗРЫВ ЦЕПОЧКИ         │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 5. Routing              сопоставляет URL+метод с endpoint              │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 6. Authentication       проверяет, кто пользователь                    │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 7. Authorization        проверяет права на ВЫБРАННЫЙ endpoint          │
  ├────────────────────────────────────────────────────────────────────────┤
  │ 8. ENDPOINT             MapGet("/test1", …) — тело обработчика         │
  └────────────────────────────────────────────────────────────────────────┘
                        ◄──  ОТВЕТ (response), в обратном порядке
                        (после 8: 7 → 6 → 5 → … → 2 → 1 → Kestrel → клиент)
```

### Пример: три компонента и порядок, который они задают

```csharp
var app = builder.Build();

app.UseExceptionHandler("/error");   // ПЕРВЫМ: должен охватывать всё, что ниже
app.UseHttpLogging();                // раньше тех, кто читает/меняет тело
app.UseStaticFiles();                // рано: статика не должна доходить до endpoint
app.UseRouting();                    // сопоставление маршрутов
app.UseAuthentication();             // сразу после Routing: нужен выбранный endpoint
app.UseAuthorization();              // сразу после Authentication

app.MapGet("/", () => "Hello World!");   // в самом конце
app.Run();
```

### Ссылки

- [ASP.NET Core middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/) — построение конвейера, `Use`, `Run`, `Map`, `MapWhen`, ветвление, short-circuiting, «UseExceptionHandler is the first middleware component added to the pipeline… Static file middleware is called early in the pipeline so that it can handle requests and short-circuit».
- API: [`ApplicationBuilder`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.applicationbuilder) — `Build()` «Produces a `RequestDelegate` that executes added middlewares».

---

## 12. Что такое endpoint (конечная точка)

### Определение (дословно близко к документации)

**Endpoint (конечная точка)** — это единица исполняемого кода обработки запроса в приложении: то, что можно **выбрать** сопоставлением URL и HTTP-метода, и **выполнить** — запустить её делегат.

Определение в документации: маршрутизация отвечает за сопоставление входящих HTTP-запросов с **исполняемыми endpoint**'ами приложения; endpoint'ы задаются в приложении и конфигурируются при его старте.

Экземпляр типа `Endpoint` обладает **четырьмя свойствами**:

| Свойство | Что означает |
|---|---|
| **Исполняемый** (executable) | Имеет `RequestDelegate` — делегат, который будет выполнен |
| **Расширяемый** (extensible) | Имеет коллекцию **метаданных** (`Metadata`) произвольного типа .NET |
| **Выбираемый** (selectable) | Может иметь (и обычно имеет) информацию о маршруте — шаблон маршрута |
| **Перечислимый** (enumerable) | Коллекцию endpoint'ов можно получить из DI через `EndpointDataSource` |

Самый распространённый тип — **`RouteEndpoint`**: он содержит шаблон маршрута, по которому система маршрутизации его выбирает.

### Где endpoint в конвейере

```
┌──────────────────────────────────────────────────────────────────┐
│ UseRouting        сопоставляет URL+метод с endpoint              │
├──────────────────────────────────────────────────────────────────┤
│ UseAuthentication │  видят, кто пользователь                     │
│ UseAuthorization  │  видят выбранный endpoint и его              │
│                   │  метаданные (например, политику              │
│                   │  авторизации) и проверяют права              │
├──────────────────────────────────────────────────────────────────┤
│ UseEndpoints      ВЫПОЛНЕНИЕ делегата выбранного endpoint        │
└──────────────────────────────────────────────────────────────────┘
  (UseEndpoints — терминальный компонент при совпадении маршрута;
   Authentication и Authorization стоят МЕЖДУ UseRouting и UseEndpoints)
```

Проверяемый факт из документации, полезный для защиты: **до вызова `UseRouting` endpoint всегда `null`**; между `UseRouting` и `UseEndpoints` — `null`, если совпадения не найдено, и заполнен, если найдено; `UseEndpoints` при совпадении — **терминальный** компонент, а код после него выполняется только когда совпадения не было.

Код для наблюдения этого эффекта:

```csharp
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    Console.WriteLine($"Endpoint: {endpoint?.DisplayName ?? "(null)"}");
    await next(context);
});

app.UseRouting();                     // до вызова — endpoint всегда (null)
app.MapGet("/", () => "Hello");       // имя endpoint'а = "Hello"
app.Run(context =>                   // терминальный middleware: сработает,
{                                    // только если endpoint не найден
    context.Response.StatusCode = 404;
    return Task.CompletedTask;
});
```

### Как создаются endpoint

```csharp
app.MapGet("/",          () => "Hello");     // GET  /
app.MapPost("/items",    item  => …);         // POST /items
app.Map("/legacy",       handler);            // ЛЮБОЙ HTTP-метод для /legacy
app.MapMethods("/both",  new[] { "OPTIONS", "HEAD" }, () => "…");

// и через полные методы:
app.MapRazorPages();
app.MapControllers();
app.MapHub<MyHub>("/chathub");               // SignalR
app.MapGrpcService<MyService>();
app.MapHealthChecks("/healthz");
```

Свойства `Map<Verb>`:

1. маршруты, добавленные напрямую в `WebApplication`, выполняются **в конце конвейера**;
2. шаблон маршрута задаёт, какие URL подходят; значения параметров извлекаются и кладутся в `HttpRequest.RouteValues`;
3. к endpoint можно прицепить метаданные, а цепочкой методов — настроить поведение:
   ```csharp
   app.MapPost("/admin", handler).RequireAuthorization("AdminOnly");
   ```

### Endpoint и middleware — в чём разница (частый вопрос)

| | Middleware | Endpoint |
|---|---|---|
| Что это | Компонент конвейера | Единица обработки запроса |
| Как подключается | `app.Use…()` | `app.MapGet()`, `app.MapPost()`, `MapControllers()` |
| Может передать дальше | Да — `await next(context)` | Нет: это конец цепочки |
| Имеет метаданные | Нет (может читать `GetEndpoint()`) | Да — `Endpoint.Metadata` |
| Выбирается по маршруту | Нет | Да — `UseRouting` сопоставляет URL |
| Типичный пример | `UseStaticFiles`, `UseAuthentication` | `MapGet("/", …)`, действие контроллера |

### Ссылки

- [Routing in ASP.NET Core → «ASP.NET Core endpoint definition»](https://learn.microsoft.com/aspnet/core/fundamentals/routing/) — четыре свойства endpoint, `RouteEndpoint`, `Endpoint.Metadata`, `EndpointDataSource`, поведение `GetEndpoint()` до/после `UseRouting`.
- API: [`EndpointRouteBuilderExtensions`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.endpointroutebuilderextensions) — `MapGet`, `MapPost`, `MapMethods`, `Map(IEndpointRouteBuilder, String, Delegate)`.

---

## 13. Каким методом middleware передаёт запрос дальше

### Краткий ответ

**Методом `Invoke` (или `InvokeAsync`)**, а передача происходит **вызовом `await next(context)`**, где `next` — параметр типа `RequestDelegate` («следующий обработчик»), переданный в middleware.

То есть это два разных понятия, и их надо различать:

| Понятие | Ответ |
|---|---|
| **Что это за метод** | `Invoke` / `InvokeAsync` — публичный метод класса-middleware либо лямбда, переданная в `app.Use(...)`. Он обязан возвращать `Task` и принимать первым параметром `HttpContext`. |
| **Как передаётся дальше** | `await next(context)` — вызов `RequestDelegate`, полученного в конструкторе (в лямбде — параметр `next`); либо `return _next(context)` |
| **Чего это стоит** | Ничего сверх обычного асинхронного вызова: цепочка — это вложенные вызовы, «луковица» |

### Минимальный код

```csharp
public class TimingMiddleware
{
    private readonly RequestDelegate _next;           // «следующий обработчик»

    public TimingMiddleware(RequestDelegate next)     // внедряется при сборке конвейера
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)     // <- передача дальше
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await _next(context);                        // <<< ВОТ ЗДЕСЬ ПЕРЕДАЁТСЯ ДАЛЬШЕ

        sw.Stop();
        context.Response.Headers["X-Time-ms"] = sw.ElapsedMilliseconds.ToString();
    }
}
```

Если передачу **не** выполнить, цепочка разорвётся — middleware станет терминальным:

```csharp
public async Task Invoke(HttpContext context)
{
    if (!context.User.Identity?.IsAuthenticated ?? false)
    {
        context.Response.StatusCode = 401;   // ответили сами, дальше НЕ пошли
        return;                              // <- а это эквивалент "не вызвать next"
    }
    await _next(context);
}
```

Инлайновый вариант (то же самое, но без класса):

```csharp
app.Use(async (context, next) =>          // next — параметр лямбды
{
    Console.WriteLine("--> " + context.Request.Path);
    await next(context);                  // передача дальше
    Console.WriteLine("<-- " + context.Request.Path);
});
```

Через `UseMiddleware<>`, когда middleware нужен как класс (в лекции по лабораторной показан именно такой вариант):

```csharp
public static class MiddlewareErrorHandlerExtensions
{
    public static IApplicationBuilder UseMiddlewareErrorHandler(this IApplicationBuilder app, string prefix = "Middleware")
        => app.UseMiddleware<MiddlewareErrorHandler>(prefix);
}

public class MiddlewareErrorHandler
{
    private readonly RequestDelegate _next;
    private readonly string _prefix;

    public MiddlewareErrorHandler(RequestDelegate next, string prefix = "Middleware")
    {
        _next = next;
        _prefix = prefix;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DivideByZeroException ex)          // частный случай — свой код ответа
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"{_prefix}: {ex}");
        }
        catch (Exception ex)                      // остальные исключения
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"{_prefix}: {ex}");
        }
    }
}
```

Подключение: `app.UseMiddlewareErrorHandler("ASPA");`

### Что ещё важно сказать про `Invoke`

- Если `next` вызывается **ни разу** — middleware терминальный.
- Если `next` вызывается **больше одного раза** — конвейер выполнит последующие компоненты дважды (для этого есть `Map`/`MapWhen`).
- Код **после** `await next(context)` выполняется на обратном пути, поэтому именно там пишут заголовки ответа и замеры.
- Параметры `InvokeAsync`, кроме `HttpContext`, заполняются из DI **на каждый запрос** — это единственный способ получить `scoped`-сервис в middleware, который создаётся один раз на всё приложение.
- Альтернатива конвенционному middleware — интерфейс **`IMiddleware`** (`InvokeAsync(HttpContext, RequestDelegate)`): активация на каждый запрос, сильная типизация.

### Ссылки

- [Write custom middleware](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/write) — «The middleware class must include: a public constructor with a parameter of type `RequestDelegate`; a public method named `Invoke` or `InvokeAsync`. This method must: return a `Task`; accept a first parameter of type `HttpContext`»; «Middleware is constructed once per *application lifetime*».
- API: [`RequestDelegate`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.http.requestdelegate) — `public delegate Task RequestDelegate(HttpContext context)`.
- [Factory-based middleware activation (IMiddleware)](https://learn.microsoft.com/aspnet/core/fundamentals/middleware/extensibility).

---

## 14. Схема обработки ошибок (ASPA002_3, задание 3)

Это тот блок, который преподаватель просит нарисовать. Ниже — и общая схема, и разбор конкретного кода лабораторной.

### 14.1. Код, который дан в задании (пп. 11–13)

Сервер имеет **5 конечных точек**; запрос на **3 из них** приводит к исключению; все исключения обрабатываются через `UseExceptionHandler`.

```csharp
internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.None);   // фильтр сообщений

        var app = builder.Build();

        app.UseExceptionHandler("/error");                     // path к обработчику Exception

        app.MapGet("/",      () => "Start ");
        app.MapGet("/test1", () =>
        {
            throw new Exception("-- Exception Test --");       // пользовательское исключение
        });
        app.MapGet("/test2", () =>
        {
            int x = 0, y = 5, z = 0;
            z = y / x;                                          // деление на 0
            return "test2";
        });
        app.MapGet("/test3", () =>
        {
            int[] x = new int[3] { 1, 2, 3 };
            int y = x[3];                                       // выход за пределы массива
            return "test3";
        });
        app.Map("/error", async (ILogger<Program> logger, HttpContext context) =>
        {
            IExceptionHandlerFeature? exobj = context.Features.Get<IExceptionHandlerFeature>();  // добираемся к Exception
            await context.Response.WriteAsync("<h1>Oops!</h1>");
            logger.LogError(exobj?.Error, "ExceptionHandler");
        });

        app.Run();
    }
}
```

| Endpoint | Что делает | Исключение | Тип исключения |
|---|---|---|---|
| `/` | возвращает `"Start "` | нет | — |
| `/test1` | явно бросает `new Exception(...)` | **да** | `System.Exception` |
| `/test2` | `z = y / x` при `x = 0` | **да** | `System.DivideByZeroException` |
| `/test3` | `x[3]` при массиве длины 3 | **да** | `System.IndexOutOfRangeException` |
| `/error` | страница-обработчик, доступ к `IExceptionHandlerFeature` | нет | — |

### 14.2. Общая ASCII-схема обработки ошибок

```
   GET /test1
      │
      ▼
┌────────────────────────────────────────────────────────────────────────────┐
│ Kestrel: формирует HttpContext, передаёт RequestDelegate                   │
└────────────────────────────────────────────────────────────────────────────┘
      │  ▼
┌────────────────────────────────────────────────────────────────────────────┐
│ [UseDeveloperExceptionPage]   ← ТОЛЬКО если среда = Development            │
│   добавляется ПЕРВЫМ автоматически; ловит всё, что ниже                    │
│   (в среде Development именно он, поэтому /error не вызывается)            │
└────────────────────────────────────────────────────────────────────────────┘
      │  ▼
┌────────────────────────────────────────────────────────────────────────────┐
│ [UseRouting]        сопоставляет /test1 → endpoint MapGet("/test1")        │
└────────────────────────────────────────────────────────────────────────────┘
      │  ▼
┌────────────────────────────────────────────────────────────────────────────┐
│ [UseExceptionHandler("/error")                                             │
│                                                                            │
│    try  {  await next(context) ──────────────┐                             │
│    } catch (Exception ex) { … обработка … }  │                             │
│                                                 │                          │
│    ◄── ИСКЛЮЧЕНИЕ ВОЗВРАЩАЕТСЯ СЮДА, ПО СТЕКУ ─┘                           │
│                                                                            │
│    Обработка:                                                              │
│      1) Request.Path = "/error"                                            │
│      2) Response.StatusCode = 500                                          │
│      3) Response.Clear() + сброс endpoint и route values                   │
│      4) Features.Set<IExceptionHandlerFeature>(Error, Path,                │
│         Endpoint)                                                          │
│      5) снимаются заголовки кэша: Cache-Control: no-cache,no-store         │
│      6) ПОВТОРНЫЙ ЗАПУСК КОНВЕЙЕРА с новым путём "/error"                  │
│      7) если исключение возникло и в обработчике —                         │
│         пробрасывается ИСХОДНОЕ исключение                                 │
└────────────────────────────────────────────────────────────────────────────┘
      │  ▼  (повторный проход конвейера, запрос теперь к /error)
┌────────────────────────────────────────────────────────────────────────────┐
│ [UseRouting]        сопоставляет /error → endpoint Map("/error")           │
└────────────────────────────────────────────────────────────────────────────┘
      │  ▼
┌────────────────────────────────────────────────────────────────────────────┐
│ ENDPOINT /error:                                                           │
│   exobj = Features.Get<IExceptionHandlerFeature>() → Error, Path           │
│   WriteAsync("<h1>Oops!</h1>")                                             │
│   logger.LogError(exobj?.Error, "ExceptionHandler")                        │
└────────────────────────────────────────────────────────────────────────────┘
      │  ▼
   HTTP 500 + тело "<h1>Oops!</h1>"  →  Kestrel  →  браузер
```

### 14.3. Тот же процесс на примере `/test2` (деление на ноль) — с обратным путём

```
ПРЯМОЙ ПУТЬ (запрос)                          ОБРАТНЫЙ ПУТЬ (ответ)
───────────────────────────────────────────────────────────────────
                                                             ┌─ UseExceptionHandler: catch ─┐
Kestrel                                                      │  Path := "/error", 500       │
  │                                                       │  Features.Set<IException..> │
UseExceptionHandler ──► try                                   │  _next(context) (повторно)  │
  │                                                       └──────────────┬──────────────┘
UseRouting ─► endpoint "/test2"                                  │
  │                                                             ▼
endpoint: z = y / x   (x = 0)  ──► ИСКЛЮЧЕНИЕ                UseRouting
                                                             │
ExceptionDispatchInfo.Capture(ex)                              │
  │  стек РАСКРУЧИВАЕТСЯ обратно:                              ▼
  └──────────────► UseExceptionHandler.catch              endpoint /error
                                                             │
                    ← WriteAsync("<h1>Oops!</h1>")            │
                    ← LogError(ex.Error, …) ── консоль        │
                                                             │
                    ◄── 500 + "<h1>Oops!</h1>" ◄───────────────┘
```

### 14.4. Разбор логики `ASPA002_3` — что происходит на каждой строке

**(1) `builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.None);`**

Фильтр логирования по **категории**. Категория логгера по умолчанию — полное имя типа, поэтому запись `CreateLogger<ExceptionHandlerMiddleware>()` даёт категорию `Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware`. Уровень `LogLevel.None` (значение 6, «Specifies that a logging category should not write any messages») полностью отключает сообщения этой категории.

Смысл именно для лабораторной: **в консоли останется только собственная строка `logger.LogError(…, "ExceptionHandler")` из endpoint `/error`**, а штатные сообщения встроенного обработчика будут скрыты. Это и есть тот «журнал, выводимый на консоль», который предлагается исследовать в п. 12 задания.

**(2) `app.UseExceptionHandler("/error");`**

Критично важно, что вызов стоит **до** всех `MapGet`. Регистрация компонента определяет его место в цепочке: всё, что зарегистрировано позже, окажется **внутри** `try/catch` обработчика ошибок.

**(3) Порядок, которое добавляет `WebApplication`**

Даже если в коде написать только `UseExceptionHandler` и `MapGet`, реальный конвейер будет таким (при наличии endpoint'ов):

```
UseDeveloperExceptionPage   ← автоматически, если среда Development
UseRouting                  ← автоматически, если настроены endpoint'ы
   UseExceptionHandler("/error")     ← пользовательский код
   MapGet("/"), MapGet("/test1..3"), Map("/error")   ← пользовательский код
UseAuthentication / UseAuthorization ← автоматически, если зарегистрированы сервисы
UseEndpoints                ← автоматически, в конце
```

Это объясняет пункт 14 задания: **«Обработчик ошибок работает корректно в режиме Release»**. В среде `Development` первым в конвейер добавляется `UseDeveloperExceptionPage`, и он перехватывает исключение раньше `UseExceptionHandler` — пользовательский `/error` просто не вызывается. Чтобы увидеть работу собственного обработчика, нужно запускать приложение **вне среды Development** (например, `ASPNETCORE_ENVIRONMENT=Production`, либо опубликованный Release-бинарник, запущенный без профиля `launchSettings.json`, где среда по умолчанию — `Production`).

> Замечание по существу: `Debug`/`Release` — это **конфигурация сборки**, а `Development`/`Production` — **среда размещения**. Они независимы. Поэтому «работает в Release» в задании — это указание на то, что при Release-запуске у вас, как правило, не подставляется `ASPNETCORE_ENVIRONMENT=Development` из `launchSettings.json`.

**(4) Что именно делает `UseExceptionHandler` (по исходному коду `ExceptionHandlerMiddlewareImpl`)**

1. Вызывает `_next(context)` внутри `try/catch` — ловит **и синхронные, и асинхронные** исключения (`await task` тоже в `try`).
2. Если клиент прервал запрос и исключение — `OperationCanceledException`/`IOException`, ставится код **499** и обработка заканчивается.
3. Если **ответ уже начат отправляться** (`Response.HasStarted`) — обработчик ничего не может сделать: исключение пробрасывается дальше, соединение закрывается.
4. Сохраняет исходный путь, подставляет `ExceptionHandlingPath = "/error"`.
5. Вызывает `Response.Clear()` и **сбрасывает endpoint и route values**, чтобы маршрутизация пересчиталась под новый путь.
6. Кладёт в `HttpContext.Features` **`IExceptionHandlerFeature`** и **`IExceptionHandlerPathFeature`** с `Error` (само исключение), `Path` (исходный путь запроса), `Endpoint`, `RouteValues`.
7. Ставит код ответа **500** (или код из `BadHttpRequestException`, либо из `StatusCodeSelector`).
8. На старте ответа снимает кэширование: `Cache-Control: no-cache,no-store`, `Pragma: no-cache`, `Expires: -1`, удаляет `ETag`.
9. Запускает зарегистрированные `IExceptionHandler` (если есть); если ни один не вернул `true` — выполняет перезапуск конвейера по пути `/error`.
10. Если исключение возникло **в самом обработчике** — пробрасывается **исходное** исключение, а не новое.
11. Логирует факт обработки. Если обработчик вернул ответ с кодом 404, а это не задумано, middleware оборачивает ситуацию в специальное исключение с пояснением; чтобы 404 считался допустимым результатом обработчика, нужно выставить `ExceptionHandlerOptions.AllowStatusCode404Response = true`.

Важные ограничения, о которых стоит упомянуть на защите (прямо из документации):

- обработчик ошибок **перезапускает конвейер**, поэтому middleware ниже должны быть готовы к повторному входу с тем же запросом (буферизовать тело или кэшировать результат чтения формы);
- в варианте `UseExceptionHandler(path)` меняется **только путь**, а route data очищается; заголовки, метод и items переиспользуются как есть;
- `scoped`-сервисы остаются теми же;
- перезапуск выполняется **исходным HTTP-методом** — если обработчик ограничен `[HttpGet]`, он выполнится только для GET; чтобы ловить все методы, обработчик не надо ограничивать атрибутом и надо разрешить анонимный доступ;
- если **заголовки ответа уже отправлены** — ни страница ошибки, ни обработчик запуститься не могут.

**(5) Почему `Map("/error", …)` срабатывает**

`Map` на `WebApplication` добавляет **`RouteEndpoint`** для указанного шаблона; при повторном запуске конвейера с путём `/error` сопоставление маршрутов находит его, и `UseEndpoints` выполняет делегат. В сигнатуре делегата `ILogger<Program>` разрешается из DI, а `HttpContext` передаётся платформой.

Возможная тонкость, о которой стоит знать (и проверить у себя): `WebApplication` реализует **и** `IApplicationBuilder`, **и** `IEndpointRouteBuilder`, у которых оба есть метод `Map` с разным смыслом. Перегрузка `Map(IApplicationBuilder, String, Action<IApplicationBuilder>)` ветвит конвейер, а `Map(IEndpointRouteBuilder, String, Delegate)` регистрирует endpoint. В приведённом коде лямбда имеет два параметра (`ILogger<Program>`, `HttpContext`), поэтому подходит именно **вторая** перегрузка — это endpoint, а не ветка конвейера. Если бы параметров не было (`app.Map("/error", async ctx => …)`), неоднозначность разрешилась бы иначе.

### Ссылки

- [Handle errors in ASP.NET Core → «Exception handler page»](https://learn.microsoft.com/aspnet/core/fundamentals/error-handling) — «Catches and logs unhandled exceptions. Re-executes the request in an alternate pipeline using the path indicated. The request isn't re-executed if the response has started»; ограничения на повторный вход; `IExceptionHandlerPathFeature`; предупреждение о вторичном исключении.
- Там же → «Developer exception page» — про `UseDeveloperExceptionPage` и авто-подключение в `Development`.
- [WebApplication и WebApplicationBuilder](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis/webapplication) — точный состав автоматически добавляемых middleware (`UseDeveloperExceptionPage` первым в `Development`, `UseRouting` вторым, `UseEndpoints` последним).
- [ASP.NET Core environments](https://learn.microsoft.com/aspnet/core/fundamentals/environments) — значение среды по умолчанию и `ASPNETCORE_ENVIRONMENT`.
- Исходный код: [`ExceptionHandlerMiddlewareImpl.cs`](https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Diagnostics/src/ExceptionHandler/ExceptionHandlerMiddlewareImpl.cs), [`ExceptionHandlerMiddleware.cs`](https://github.com/dotnet/aspnetcore/blob/main/src/Middleware/Diagnostics/src/ExceptionHandler/ExceptionHandlerMiddleware.cs).
- API: [`IExceptionHandlerPathFeature`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.diagnostics.iexceptionhandlerpathfeature), [`UseStatusCodePages`](https://learn.microsoft.com/dotnet/api/microsoft.aspnetcore.builder.statuscodepagesextensions).

---

# Часть 3. Приложения

## Приложение А. Сводка: как задания лабораторной связаны с теорией

| Задание | Что требуется | Чем реализуется | Где объяснено в этом документе |
|---|---|---|---|
| 1, п. 2 | приветственная страница на `/aspnetcore` | `app.UseWelcomePage("/aspnetcore")` | п. 6 (список встроенных), п. 3 |
| 1, п. 2 | почему `MapGet("/aspnetcore", …)` ниже не сработает | `WelcomePageMiddleware` **не вызывает** `_next` для совпавшего пути → терминальный компонент, цепочка разорвана | п. 2, п. 3 |
| 1, п. 3 | статические файлы на `/` | `app.UseStaticFiles()` | п. 4, п. 7 |
| 1, п. 5 | `Index.html` — стартовая страница | `app.UseDefaultFiles()` **перед** `UseStaticFiles()` | п. 6 |
| 2, п. 9.1 | `Neumann.html` стартовая, список имён изменить | `DefaultFilesOptions`: `Clear()` + `Add("Neumann.html")` | п. 6 |
| 2, п. 9.2 | статика на `/static` | `UseStaticFiles(new StaticFileOptions { RequestPath = "/static", … })` | п. 4, п. 7 |
| 2, п. 9.3 | доп. каталог `Picture` | `new PhysicalFileProvider(Path.Combine(ContentRootPath, "Picture"))` | п. 4, п. 7 |
| 3, пп. 11–14 | 5 endpoint, 3 исключения, `UseExceptionHandler` | `app.UseExceptionHandler("/error")` + `IExceptionHandlerFeature` | п. 14 |
| 4, пп. 15–22 | вопросы | Builder, Chain of Responsibility, Middleware, статический файл, `Use*`, список middleware, `wwwroot`, фон Нейман | пп. 1–8 |

### Код `UseDefaultFiles` с изменённым списком имён (задание 2, п. 9.1)

```csharp
var options = new DefaultFilesOptions();
options.DefaultFileNames.Clear();                 // убрать default.htm, default.html, index.htm, index.html
options.DefaultFileNames.Add("Neumann.html");     // задать Neumann.html единственной стартовой
app.UseDefaultFiles(options);                     // ДО UseStaticFiles()
app.UseStaticFiles();
```

### Код `UseWelcomePage` (задание 1, п. 2) — и почему `MapGet` ниже не сработает

```csharp
var app = builder.Build();

app.UseWelcomePage("/aspnetcore");                // терминальный для пути /aspnetcore

app.MapGet("/aspnetcore", () => "этот код не выполнится");
```

Логика `WelcomePageMiddleware` (исходный код): если путь запроса совпал с заданным — middleware **формирует и отдаёт приветственную страницу и не вызывает `_next`**. Дальше по цепочке (включая `UseEndpoints` и `MapGet`) управление не передаётся. Поэтому обработчик `MapGet("/aspnetcore", …)` физически недостижим при таком порядке регистрации — это и есть ответ на вопрос задания.

---

## Приложение Б. Типичный порядок регистрации middleware

Порядок «критичен для безопасности, производительности и функциональности». Практический шаблон:

```csharp
var app = builder.Build();

// 1. Обработка ошибок — первыми, чтобы охватить всё остальное
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
    app.UseExceptionHandler("/Error");

// 2. Строгая транспортная безопасность
app.UseHsts();

// 3. Принудительное HTTPS
app.UseHttpsRedirection();

// 4. Заголовки от прокси — раньше тех, кто читает схему/хост/IP
app.UseForwardedHeaders();

// 5. Логирование запросов и ответов
app.UseHttpLogging();

// 6. Статика — рано, чтобы разорвать цепочку при совпадении
app.UseDefaultFiles();     // ДО UseStaticFiles
app.UseStaticFiles();

// 7. Маршрутизация
app.UseRouting();

// 8. Аутентификация и авторизация — сразу после Routing, до endpoint'ов
app.UseAuthentication();
app.UseAuthorization();

// 9. Endpoint'ы
app.MapGet("/", () => "Hello World!");
app.MapRazorPages();

app.Run();
```

Правила, которые чаще всего нарушают:

| Правило | Нарушение | Последствие |
|---|---|---|
| `UseExceptionHandler` / `UseDeveloperExceptionPage` — первыми | поставить ниже `UseStaticFiles` | исключения внутри статики/логирования останутся необработанными |
| `UseDefaultFiles` — до `UseStaticFiles` | поменять местами | стартовая страница не отдаётся |
| `UseStaticFiles` — рано | после `UseRouting` | лишняя работа маршрутизации для каждого файла |
| `UseAuthentication` — до `UseAuthorization` | наоборот | авторизация без аутентификации не увидит пользователя |
| `UseCors` — до `UseAuthentication`/`UseAuthorization`/`UseResponseCaching` | позже | CORS-заголовки не применятся |
| `UseRequestTimeouts` — после обработки ошибок и `UseRouting` | раньше | таймаут перехватит исключение раньше обработчика |

---

## Приложение В. Карта «вопрос → источник в документации»

Быстрый список для повторения перед защитой.

| Вопрос | Раздел документации |
|---|---|
| Что такое middleware, терминальный middleware | `aspnetcore/fundamentals/middleware/` |
| Полный список встроенных middleware | `aspnetcore/fundamentals/middleware/#built-in-middleware` |
| Как написать свой middleware (`Invoke`/`InvokeAsync`, `UseMiddleware<>`) | `aspnetcore/fundamentals/middleware/write` |
| Активация middleware через фабрику (`IMiddleware`) | `aspnetcore/fundamentals/middleware/extensibility` |
| Статические файлы, `wwwroot`, доп. каталоги | `aspnetcore/fundamentals/static-files` |
| Web root и content root | `aspnetcore/fundamentals/#web-root` |
| Обработка ошибок, `UseExceptionHandler`, `IExceptionHandlerPathFeature` | `aspnetcore/fundamentals/error-handling` |
| Endpoint: четыре свойства, метаданные, `UseRouting`/`UseEndpoints` | `aspnetcore/fundamentals/routing` |
| `WebApplicationBuilder`, автоматически добавляемые middleware | `aspnetcore/fundamentals/minimal-apis/webapplication` |
| Среда размещения, `ASPNETCORE_ENVIRONMENT` | `aspnetcore/fundamentals/environments` |
| OWIN и адаптеры `Microsoft.AspNetCore.Owin` | `aspnetcore/fundamentals/owin` |
| Katana: IIS и self-hosting | `aspnet/aspnet/overview/owin-and-katana/` |
| Kestrel, HTTP.sys, модели размещения | `aspnetcore/fundamentals/servers/` |
| Переписывание URL | `aspnetcore/fundamentals/url-rewriting` |
| Логирование запросов/ответов | `aspnetcore/fundamentals/http-logging` |

---

# Часть 4. Вопросы, которые задаст преподаватель сверх списка

## Доп. 1. Чем middleware отличается от фильтра (filter) и от endpoint?

- **Middleware** — компонент конвейера, работает **до** маршрутизации и до выбора обработчика, ничего не знает о маршруте; может разорвать цепочку.
- **Filter** (MVC: `IActionFilter`, `IExceptionFilter`, `IResultFilter`) — механизм MVC, работает **внутри** обработки запроса, уже зная, какой контроллер и действие выбраны; может отменить выполнение или заменить результат.
- **Endpoint** — конечная точка, единица исполняемого кода, выбирается маршрутизацией и выполняется в конце конвейера.
- Практическое правило: «сквозная функциональность» → middleware; «логика вокруг конкретного действия» → filter; «сама обработка запроса» → endpoint.

## Доп. 2. Что произойдёт, если `UseExceptionHandler` поставить **после** `MapGet`?

`MapGet` не добавляет middleware — он регистрирует endpoint, а `UseEndpoints` выполняется в конце конвейера. Middleware, зарегистрированные в коде, попадают между `UseRouting` и `UseEndpoints`. Поэтому `UseExceptionHandler`, написанный после `MapGet`, всё равно окажется **выше** по конвейеру, чем выполнение endpoint'а, и исключение из `MapGet` будет перехвачено. Однако код становится непонятным и хрупким: при добавлении терминальных компонентов (`UseStaticFiles` до маршрутизации, `Run`) порядок может сломаться. **Правило: обработку ошибок регистрировать первой явно.**

## Доп. 3. Почему `UseDefaultFiles` нельзя вызвать после `UseStaticFiles`?

`UseDefaultFiles` — не «раздатчик файлов», а **переписыватель URL**: он меняет путь запроса с `/` на `/index.html`, если такой файл есть, и передаёт запрос дальше. Если `UseStaticFiles` уже стоит выше, он увидит запрос к `/`, файл `index.html` не найдёт, разорвать цепочку не сможет и передаст запрос дальше — а `UseDefaultFiles` ниже уже не выполнится, потому что цепочка ушла в endpoint. **Правильный порядок: `UseDefaultFiles()`, затем `UseStaticFiles()`.**

## Доп. 4. Что такое short-circuiting и зачем он нужен? Назовите терминальные middleware из лабораторной.

**Short-circuiting (короткое замыкание)** — обработчик не вызывает следующий компонент и сам формирует ответ, завершая конвейер. Экономит время и память: дальнейшие компоненты просто не выполняются.

Терминальные в лабораторной:

| Компонент | Когда становится терминальным |
|---|---|
| `UseStaticFiles()` | когда нашёл и отдал файл |
| `UseWelcomePage(path)` | когда путь запроса совпал с `path` |
| `UseAuthentication()` | для OAuth-callback-ов |
| `UseHealthChecks()` | когда запрос попал в health-check endpoint |
| `UseEndpoints(...)` | когда маршрут сопоставлен с endpoint'ом |
| `Run(...)` | всегда — это и есть его назначение |
| `UseDirectoryBrowser()` | когда каталог показан |

## Доп. 5. Чем `UseStatusCodePages` отличается от `UseExceptionHandler`?

- `UseExceptionHandler` работает с **исключениями**: ловит необработанное исключение, логирует его и перезапускает конвейер по другому пути.
- `UseStatusCodePages` работает с **кодами состояния**: если приложение выставило 400–599 и не записало тело, middleware добавляет текстовую заглушку («Status Code: 404; Not Found»). Исключения он **не ловит** вообще.
- Есть три формы: без параметров (текст по умолчанию), с форматной строкой, с лямбдой; плюс варианты `WithRedirects` (302 на страницу ошибки) и `WithReExecute` (тело формируется повторным запуском конвейера, исходный код и адресная строка сохраняются).
- Важно: `UseStatusCodePages` вызывают **до** middleware обработки запросов (в частности до статики и endpoint'ов), и в продакшене обычно не используют — сообщение не полезно пользователю.

## Доп. 6. Что такое `IExceptionHandlerFeature` и чем отличается от `IExceptionHandlerPathFeature`?

Оба клавиши в `HttpContext.Features`, которые кладёт `ExceptionHandlerMiddleware` при перехвате исключения.

| | `IExceptionHandlerFeature` | `IExceptionHandlerPathFeature` |
|---|---|---|
| Что даёт | ссылка на **само исключение** (`Error`), а также `Endpoint` и `RouteValues` | исходный **путь запроса** (`Path`), до подстановки `/error` |
| Когда доступны | в обработчике ошибок | в обработчике ошибок |

Возможности интерфейса на момент написания документации: `Error`, `Path`, `Endpoint`, `RouteValues`. Практический вывод для `ASPA002_3`: `context.Features.Get<IExceptionHandlerFeature>()` — это «добраться до Exception», чтобы залогировать его на своей странице ошибки. **И никогда не отдавать сведения об исключении клиенту** — документация прямо предупреждает, что это риск безопасности.

## Доп. 7. Как отличить `Debug` от `Release` и почему обработчик ошибок «работает в Release»?

- `Debug` / `Release` — **конфигурация сборки**: в `Debug` нет оптимизации IL, определены константы `DEBUG` и `TRACE`, работают `Debug.Assert` и `Debug.WriteLine`; в `Release` включена оптимизация, ассемблирование, сворачивание кода, точки останова смещаются.
- `Development` / `Production` — **среда размещения** (`ASPNETCORE_ENVIRONMENT`), от неё зависит поведение: в `Development` автоматически добавляется `UseDeveloperExceptionPage`, в остальных — `UseExceptionHandler`.
- Поэтому фраза задания «обработчик ошибок работает корректно в режиме Release» означает: **запускайте вне среды `Development`** — например, опубликованный бинарник без `launchSettings.json` (тогда среда по умолчанию `Production`), либо явно задайте `ASPNETCORE_ENVIRONMENT=Production`. В среде `Development` перехватывает `UseDeveloperExceptionPage`, стоящий в конвейере первым, и ваш `/error` не вызывается.

## Доп. 8. Что такое `UseWelcomePage` и чем он отличается от `MapFallbackToFile`?

- `UseWelcomePage(path)` — встроенный middleware (`WelcomePageMiddleware`, сборка `Microsoft.AspNetCore.Diagnostics`), который для совпавшего пути **генерирует приветственную страницу** и **не вызывает следующий компонент** (терминальный).
- `MapFallbackToFile(path)` — регистрирует **endpoint**-«заглушку»: если ни один маршрут не совпал, запрос направляется на указанный файл (обычно `index.html`). Он не терминальный «по построению» — он участвует в маршрутизации и выполняется, когда другие маршруты не подошли.
- Вывод: `UseWelcomePage` — про «показать стартовую страницу нового приложения», `MapFallbackToFile` — про «отдать клиенту SPA/страницу по умолчанию для любого неизвестного URL».

---

*Документ подготовлен для защиты лабораторной работы № 2. Все фактические утверждения сверены с документацией Microsoft Learn по ASP.NET Core и с исходным кодом `dotnet/aspnetcore`; ссылки приведены по месту каждого утверждения.*
