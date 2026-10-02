# Лекция 06. MIDDLEWARE / ERRORHANDLER (обработка ошибок)

## Назначение
Middleware-конвейер в ASP.NET Core и три способа обработки исключений: **UseExceptionHandler**,
**inline `app.Use`** с `try/catch`, и **класс middleware через `UseMiddleware<>`** с собственным
расширением `IApplicationBuilder`. Лекция почти целиком из скриншотов (697 символов текста
на 6 страниц) — весь код снят со слайдов.

## 1. Структура ASP.NET CORE-приложения (схема, слайд 1)
```
Host
  OWIN Server
    User Agent <-> Some Middleware <-> Some Other Middleware <-> Application
```

## 2. Структура middleware, стек вызовов (схема «луковица», слайд 1)
```
Request
  -> Middleware-1  (Modify Request -> await next();)
  -> Middleware-2  (Modify Request -> await next();)
  -> Middleware-n  (Modify Request)
  -> Application Codes (Controllers/Action)
Response
  <- Modify Response (Middleware-n)
  <- Modify Response (Middleware-2)
  <- Modify Response (Middleware-1)
```
**Принцип:** каждый middleware выполняет код **до** `await next()` на пути запроса и **после** него
на пути ответа (луковица / onion model).

## 3. Встроенные middleware-элементы — код со слайда
```csharp
private static void Main(string[] args)
{
    Data data = new Data();

    var builder = WebApplication.CreateBuilder(args);
    builder.Services.AddControllersWithViews();
    var app = builder.Build();
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Pipe}/{action=Index}/{id?}");
    app.Run();
}
```
Красным подчёркнуты (обязательные/ключевые вызовы): `app.UseExceptionHandler("/Home/Error")`,
`app.UseStaticFiles()`, `app.UseRouting()`, `app.UseAuthorization()`, `app.Run()`.

## 4. Порядок middleware-элементов важен (схема конвейера, слайд 2)
```
Response <- ExceptionHandler -> HSTS -> HttpsRedirection -> Static Files -> Routing
          -> CORS -> Authentication -> Authorization -> Custom middlewares
          (Custom1 ... Custom ...) -> Endpoint
```
Элемент, подключённый **раньше** (выше в списке), обслуживает запрос первым;
при обработке ответа порядок обратный.

## 5. UseExceptionHandler — традиционная схема обработки ошибок (слайд 2)
```csharp
app.UseExceptionHandler("/Celebrities/Error");

app.MapGet("/Celebrities", () => repository.getAllCelebrities());
app.MapGet("/Celebrities/{id:int}", (int id) => {...});
app.MapPost("/Celebrities", (Celebrity celebrity) => {...})
.AddEndpointFilter(async (context, next) => {...})
.AddEndpointFilter(async (context, next) => {...})
.AddEndpointFilter(async (context, next) => {...});
app.MapPut("/Celebrities/{id:int}", (int id, Celebrity celebrity) => {...});
app.MapDelete("/Celebrities/{id:int}", (int id) => {...});

app.MapFallback((HttpContext ctx) => Results.NotFound(new { error = $"path {ctx.Request.Path} not supported" }));

app.Map("/Celebrities/Error", (HttpContext ctx) => {...});
```

## 6. Схема обработки ошибок с помощью middleware-элемента (слайд 3)
```csharp
app.Use(async (context, next) => {...});

app.MapGet("/Celebrities", () => repository.getAllCelebrities());
app.MapGet("/Celebrities/{id:int}", (int id) => {...});      // здесь throw Exception (выделено красным)
app.MapPost("/Celebrities", (Celebrity celebrity) => {...})
.AddEndpointFilter(async (context, next) => {...})
.AddEndpointFilter(async (context, next) => {...})
.AddEndpointFilter(async (context, next) => {...});
app.MapPut("/Celebrities/{id:int}", (int id, Celebrity celebrity) => {...});
app.MapDelete("/Celebrities/{id:int}", (int id) => {...});

app.MapFallback((HttpContext ctx) => Results.NotFound(new { error = $"path {ctx.Request.Path} not supported" }));
```
Красная стрелка идёт от внешнего `app.Use(async (context, next) => {...})` к тексту
**«throw Exception»** — обработка замкнута на внешний `app.Use`, охватывающий все конечные точки.

## 7. Middleware-элемент: полный код обработчика (слайд 3)
```csharp
app.Use(async (context, next) =>
{
    try { await next.Invoke(); }
    catch (AbsurdeException ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Middleware:" + ex.ToString());
    }
    catch (NotFoundByIdException ex)
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("Middleware:" + ex.ToString());
    }
    catch (ValueException ex)
    {
        context.Response.StatusCode = 409;
        await context.Response.WriteAsync("Middleware:" + ex.ToString());
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("Middleware:" + ex.ToString());
    }
});
```
**Порядок `catch` важен:** сначала самые специфичные исключения, последним — `catch (Exception ex)`.
Иначе (е бы `catch (Exception)` стоял первым) обработчик никогда не дошёл бы до 404/409.

### Соответствие кодов ответа исключениям
| Исключение | StatusCode |
|---|---|
| `AbsurdeException` | **500** |
| `NotFoundByIdException` | **404** |
| `ValueException` | **409** |
| `Exception` (любое) | **500** |

### Конечные точки и фильтры на слайде 7
```csharp
app.MapGet("/Celebrities/{id:int}", (int id) => {
    Celebrity? celebrity = repository.getCelebrityById(id);
    if (celebrity == null) throw new NotFoundByIdException($"/Celebrities, Celebrity Id = {id}");
    return celebrity;
});

app.MapPost("/Celebrities", (Celebrity celebrity) => {...})
.AddEndpointFilter(async (context, next) =>
{
    Celebrity? celebrity = context.GetArgument<Celebrity>(0);   // получить самый левый (0й) параметр из лямбды в MapPost
    if (celebrity == null) throw new AbsurdeException("POST /Celebrities error, loc = 001");
    if (celebrity.Surname == null || celebrity.Surname.Length <= 1) throw new ValueException("POST /Celebrities error, Surname is wrong");
    return await next(context);
})
.AddEndpointFilter(async (context, next) => {...})
.AddEndpointFilter(async (context, next) => {...});

app.MapPut("/Celebrities/{id:int}", (int id, Celebrity celebrity) => {...});
app.MapDelete("/Celebrities/{id:int}", (int id) => {...});
```
Ключевой приём: **`context.GetArgument<Celebrity>(0)`** — из объекта контекста фильтра
достаётся самый левый (0-й) параметр лямбды-обработчика `MapPost` (т.е. сам `celebrity`).

## 8. UseMiddleware<> — класс middleware (слайд 4)
```csharp
public class MiddlewareErrorHandler
{
    private readonly RequestDelegate _next;
    private readonly string          _prefix;
    public MiddlewareErrorHandler(RequestDelegate next, string prefix = "Middleware")
    {
        this._next = next;
        this._prefix = prefix;
    }
    public async Task InvokeAsync(HttpContext context)
    {
        try { await this._next(context); }
        catch (AbsurdeException ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"{this._prefix}:{ex.ToString()}");
        }
        catch (NotFoundByIdException ex)
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync($"{this._prefix}:{ex.ToString()}");
        }
        catch (ValueException ex)
        {
            context.Response.StatusCode = 409;
            await context.Response.WriteAsync($"{this._prefix}:{ex.ToString()}");
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync($"{this._prefix}:{ex.ToString()}");
        }
    }
}
```
Ключевые детали конструктора:
- принимает `RequestDelegate next` (следующий элемент конвейера) и строку `prefix = "Middleware"`;
- конструктор с параметрами -> `app.UseMiddleware<MiddlewareErrorHandler>("ANC23")` передаёт их через DI;
- метод обработки называется **`InvokeAsync`** (не `Invoke`) — асинхронный.

### Регистрация и конечные точки (слайд 4)
```csharp
app.UseMiddleware<MiddlewareErrorHandler>("ANC23");

app.MapGet("/Celebrities", () => repository.getAllCelebrities());      // ASPA03

app.MapGet("/Celebrities/{id:int}", (int id) => {
    Celebrity? celebrity = repository.getCelebrityById(id);
    if (celebrity == null) throw new NotFoundByIdException($"/Celebrities, Celebrity Id = {id}");
    return celebrity;
});

app.MapPost("/Celebrities", (Celebrity celebrity) => {...})
.AddEndpointFilter(async (context, next) =>
{
    Celebrity? celebrity = context.GetArgument<Celebrity>(0);   // получить самый левый (0й) параметр из лямбды в MapPost
    if (celebrity == null) throw new AbsurdeException("POST /Celebrities error, loc = 001");
    if (celebrity.Surname == null || celebrity.Surname.Length <= 1) throw new ValueException("POST /Celebrities error, Surname is wrong");
    return await next(context);
})
.AddEndpointFilter(async (context, next) => {...})
.AddEndpointFilter(async (context, next) => {...});
app.MapPut("/Celebrities/{id:int}", (int id, Celebrity celebrity) => {...});
app.MapDelete("/Celebrities/{id:int}", (int id) => {...});
```

## Результат в Postman (скриншот, слайд 5)
Запрос: `GET http://localhost:5030/Celebrities/25555` -> **404 Not Found**, тело:
```
ANC23:Program+NotFoundByIdException: Not Found by Id: /Celebrities, Celebrity Id = 25555
   at Program.<>c__DisplayClass0_0.<Main>b__1(Int32 id) in D:\VS_Projects\ANC\ANC23\Program.cs:line 54
   at lambda_method2(Closure, Object, HttpContext)
   at Microsoft.AspNetCore.Routing.EndpointMiddleware.Invoke(HttpContext httpContext)
   -- End of stack trace from previous location ---
   at ANC23.MiddlewareErrorHandler.InvokeAsync(HttpContext context) in D:\VS_Projects\ANC\ANC23\MiddlewareErrorHandler.cs:line 17
```
Префикс **`ANC23:`** в начале ответа — это второй параметр, переданный в
`UseMiddleware<MiddlewareErrorHandler>("ANC23")`.

## 9. Расширение класса IApplicationBuilder (слайд 5)
```csharp
public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseANCErrorHandler(this IApplicationBuilder app, string prefix)
    {
        return app.UseMiddleware<MiddlewareErrorHandler>(prefix);
    }
}
```
Два эквивалентных способа регистрации:
```csharp
app.UseMiddleware<MiddlewareErrorHandler>("ANC23");   // напрямую
app.UseANCErrorHandler("ANC23");                      // через собственное расширение
```

## Три способа обработки ошибок — сравнение

| Способ | Где регистрируется | Плюсы |
|---|---|---|
| `app.UseExceptionHandler(path)` + `app.Map(path, handler)` | Program.cs | стандартный механизм, `IExceptionHandlerFeature`, разделение на endpoint |
| `app.Use(async (context, next) => { try/catch })` | Program.cs | быстро, без класса; тело в Program.cs |
| `app.UseMiddleware<T>(prefix)` (класс) | отдельный файл класса + `IApplicationBuilder` extension | переиспользуемо, настраиваемо через конструктор, используется в лабе 8 (`UseCelebritiesErrorHandler("ANC28")`) |

Лабы, где это используется:
- **Лаба 2**: `UseExceptionHandler("/error")` + `Map("/error", ...)` с `IExceptionHandlerFeature`
- **Лаба 4**: `UseExceptionHandler("/Celebrities/Error")` + `Results.Problem/BadRequest`
- **Лаба 5**: `UseExceptionHandler("/Celebrities/Error")` (фильтры бросают исключения)
- **Лаба 6**: middleware-обработчик ошибок (п.16) -> коды `#404002:Celebrity Id = 4`, `#404004:Lifeevent Id = 10`
- **Лаба 8**: `app.UseCelebritiesErrorHandler("ANC28")` — расширение из библиотеки

## Ключевые API и типы (сводно)

| Имя | Назначение |
|---|---|
| `app.Use(...)` | подключение inline-middleware |
| `app.UseMiddleware<T>(params)` | подключение класса-middleware, передача параметров в конструктор |
| `RequestDelegate` | делегат следующего элемента конвейера |
| `InvokeAsync(HttpContext)` / `Invoke(HttpContext)` | метод обработки middleware |
| `IApplicationBuilder` | интерфейс для сборки конвейера |
| `IApplicationBuilder.UseXXX(this IApplicationBuilder, ...)` | собственное расширение |
| `HttpContext`, `Response.StatusCode`, `Response.WriteAsync(...)` | формирование ответа |
| `IExceptionHandlerFeature` | `context.Features.Get<IExceptionHandlerFeature>()?.Error` |
| `UseExceptionHandler`, `Map`, `MapFallback` | обработка ошибок и 404 |
| `AddEndpointFilter(...)` | фильтры конечной точки (в т.ч. бросающие исключения) |
| `context.GetArgument<T>(0)` | параметр лямбды-обработчика из контекста фильтра |
| `await next.Invoke()` / `await next(context)` | передача управления дальше |
| `UseStaticFiles`, `UseRouting`, `UseAuthorization`, `UseHsts`, `UseHttpsRedirection` | встроенные middleware |
| `AddControllersWithViews()`, `MapControllerRoute(...)` | MVC в конвейере |
| `app.Environment.IsDevelopment()` | проверка режима среды |

## Источник
- `lectures/Лекция_06_ASPNETCORE_MIDDLEWARE_ERRORHANDLER.pdf` — стр. 1 (слайд 1: схема
  «Host -> OWIN Server -> Middleware -> Application»; слайд 2: схема «луковица» middleware;
  слайд 3: код встроенных middleware элементов с подчёркнутыми вызовами),
  стр. 2 (слайд 4: схема порядка middleware-конвейера; слайд 5: код `UseExceptionHandler`
  традиционной схемы), стр. 3 (слайд 6: схема обработки ошибок через `app.Use` с красной стрелкой;
  слайд 7: полный код inline-обработчика с try/catch и коды 500/404/409/500,
  `context.GetArgument<Celebrity>(0)`, фильтры `MapPost/MapPut/MapDelete`),
  стр. 4 (слайд 8: класс `MiddlewareErrorHandler` с `RequestDelegate _next`, `_prefix`,
  конструктором и `InvokeAsync`; регистрация `app.UseMiddleware<MiddlewareErrorHandler>("ANC23")`),
  стр. 5 (скриншот Postman с 404 и стеком вызовов; слайд 9: `MiddlewareExtensions` с
  `UseANCErrorHandler`; «КОНЕЦ»), стр. 6 (пустая).