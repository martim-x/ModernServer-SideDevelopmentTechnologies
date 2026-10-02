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
