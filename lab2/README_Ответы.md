# Ответы на вопросы по лабораторной № 2

---

## 1. Типы middleware

| Тип | Признак | Пример из работы |
|---|---|---|
| **Встроенный (built-in)** | метод `Use<Name>()` из `Microsoft.AspNetCore.Builder` | `UseWelcomePage`, `UseStaticFiles`, `UseDefaultFiles`, `UseExceptionHandler` |
| **Inline (лямбда)** | `app.Use(async (context, next) => ...)` | не использован в лабораторной, но показан в лекции 6 (`try/catch` с `await next()`) |
| **Класс (`IMiddleware` / `UseMiddleware<T>`)** | публичный конструктор с `RequestDelegate`, метод `Invoke` или `InvokeAsync` | `MiddlewareErrorHandler` (лекция 6, слайд 4) |
| **Расширение (`IApplicationBuilder`)** | собственный метод `Use<Name>(this IApplicationBuilder, ...)` | `UseANCErrorHandler` (лекция 6, слайд 5) |

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
