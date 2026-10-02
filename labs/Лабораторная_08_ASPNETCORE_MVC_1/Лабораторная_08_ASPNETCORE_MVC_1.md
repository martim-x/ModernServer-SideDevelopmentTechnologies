# Лабораторная 8. ASPA — MVC (модель-представление-контроллер)

## Назначение
Перевести приложение знаменитостей на архитектуру **MVC**: контроллер `CelebritiesController`,
представления Razor с использованием **Layout**, **Action-фильтр** для подгрузки данных
из Википедии и Facebook, HTML-helper для отображения фотографии.

## Обозначения в ТЗ
> **♣ — задания, требующие самостоятельного изучения (нет в лекциях)**

♣ отмечено в шапке лабораторной.

## Общие указания (дословно из шапки)
- Используйте библиотеку **DAL_Celebrity_MSSQL**, разработанную в предыдущем задании.
- Оформление html-разметки — **на усмотрение работника**.

## Требуемые проекты
- Имя решения: **ASPA**
- Имя проекта: **ASPA008_1**
- Версия .NET: **7.0 или 8.0**
- Шаблон: **Веб-приложение ASP.NET Core (модель-представление-контроллер) (Майкрософт)**
  — «Шаблон проекта для создания приложения ASP.NET Core с образцом представлений MVC и
  контроллером ASP.NET Core. Этот шаблон можно также использовать для служб HTTP REST и API.»

## Задание 1
1. Разработайте ASPA, применив следующий шаблон.
   Имя решения: **ASPA**, имя проекта: **ASPA008_1**, версия .NET: 7.0 или 8.0.
2. Структура проекта приведена и пояснена ниже.

### Структура решения ANC28 (скриншот обозревателя решений, стр. 1)
```
ANC28
├── Connected Services
├── Properties
├── wwwroot
├── Зависимости
│   ├── Анализаторы
│   ├── Пакеты
│   ├── Платформы
│   └── Проекты
│       ├── ANC25_WEBAPI_DLL
│       └── DAL_Celebrity_MSSQL
└── DAL_Celebrity_
```

### Пояснение структуры (таблица ТЗ, стр. 1)

| Проект/элемент | Назначение (дословно) |
|---|---|
| **ANC25_WEBAPI_DLL** | Библиотека, содержащая: сервисы: `IRepository` (репозиторий БД Celebrities), `IOptions<CelebritiesConfig>` (конфигурация), `CountryCodes` (ISO-коды стран), `CelebrityTitles` (заголовки представлений); - конечные точки: `MapGet`, `MapPost`, `MapPut`, `MapDelete` - разработанные в предыдущих лабораторных работах; - `middleware-обработчик ошибок MiddlewareErrorHandler` |
| **DAL_Celebrity_MSSQL** | Библиотека, разработанная в предыдущих лабораторных работах |
| **DAL_Celebrity_** | Библиотека, разработанная в предыдущих лабораторных работах |

### Структура проекта (продолжение, стр. 2)
```
├── Controllers
│   └── CelebritiesController
│       └── CelebritiesController.cs
├── CountryCodes
│   └── iso3166-1-alpha2-country-codes.json
├── Filters
│   └── InfoOfCelebrityAsyncActionFilter.cs
├── Helpers
│   └── CelebrityPhoto.cs
├── Models
├── Photos
│   └── Celebrities
├── Views
│   ├── Celebrities
│   ├── Shared
│   │   ├── ViewImports.cshtml
│   │   └── _Layout.cshtml
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── app.http
│   ├── appsettings.json
│   ├── Celebrities.config.json
│   └── Program.cs
```

### Пояснение элементов структуры (таблица ТЗ, стр. 2)

| Элемент | Назначение (дословно) |
|---|---|
| **CountryCodes** | Директорий с json-файлом с кодами стран (прилагается) |
| **Controllers** | Контроллер `CelebritiesController`, выполняющий: - отображение фотографий всех знаменитостей; - отображение информации выбранной знаменитости; - добавление информации о новой (с подтверждением) знаменитости; - удаление информации о существующей знаменитости (с подтверждением) |
| **Filters** | Action-фильтр для метода контроллера (добавление ссылок о знаменитости из сервиса Википедия), отображающего знаменитость |
| **Helpers** | HTML-helper `CelebrityPhoto` для отображения фотографии знаменитости |
| **Photos** | Директорий с файлами-фотографиями знаменитостей |
| **Celebrities.config.json** | Конфигурационный файл приложения |
| **Views/Celebrities** | Заголовки представлений (метки страниц приложения) |
| **Views/Shared** | Общие представления (макет Layout) |

3. Примерный вид файла `Program.cs`:
4. Примерный вид файла конфигурации:
5. Примерный вид приложения при запуске.

## Код Program.cs для MVC (задание 3) — со скриншота, полностью читаем
```csharp
private static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    builder.AddCelebritiesConfiguration();   // подключение конфигурации IOptions<CelebritiesConfig>
    builder.AddCelebritiesServices();         // подключение сервисов: IRepository, CountryCodes, CelebrityTitles
    builder.Services.AddControllersWithViews();
    var app = builder.Build();
    app.UseHttpsRedirection();
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
    }
    app.UseStaticFiles();
    app.UseRouting();

    app.UseCelebritiesErrorHandler("ANC28");   // подключение обработчика ошибок
    app.MapCelebrities();                     // Map для Celebrities
    app.MapLifeevents();                      // Map для Lifeevents
    app.MapPhotoCelebrities();                // Map для фотографий

    app.UseAuthorization();

    // маршрутизация
    app.MapControllerRoute(                            // добавление новой знаменитости
        name: "celebrity",
        pattern: "/0",
        defaults: new { Controller = "Celebrities", Action = "NewHumanForm" });
    app.MapControllerRoute(                            // действия с одной знаменитостью
        name: "celebrity",
        pattern: "/{id:int:min(1)}",
        defaults: new { Controller = "Celebrities", Action = "Human"});
    app.MapControllerRoute(                            // отображение всех знаменитостей или одной (id)
        name: "default",
        pattern: "{controller=Celebrities}/{action=Index}/{id?}");

    app.Run();
}
```

### Файл конфигурации (задание 4) — со скриншота
```jsonc
// Celebrities.config.json
"Celebrities": {
    "PhotosRequestPath": "/Photos",
    "PhotosFolder":       "D:\\VS_Projects\\ANC\\ANC28\\Photos",
    "ConnectionString":   "Data source=172.16.193.88; Initial Catalog=LES28X;TrustServerCertificate=True;User Id=[закрашено];Password=[закрашено]",
    "ISO3166Alpha2Path":  "D:\\VS_Projects\\ANC\\ANC28\\CountryCodes\\iso3166-1-alpha2-country-codes.json"
}
```
Добавленный по сравнению с лабой 7 ключ: **`ISO3166Alpha2Path`** — путь к json-файлу кодов стран ISO 3166-1 alpha-2.
Файл `iso3166-1-alpha2-country-codes.json` прилагается к лабе (16 КБ).

## Внешний вид приложения (задание 5)
Заголовок **Celebrities Dictionary Internet Service**, ссылки **Home** / **Life Events**,
раздел **Celebrities**, сетка фотографий, подпись **Copyright 6STU Development**
(аналогично лабе 7).

## Задания по видам форм
6. Примерный вид форм (ввод и подтверждение) для ввода информации о новой знаменитости.
7. Примерный вид формы для просмотра информации о выбранной знаменитости
   (на скриншоте — Donald Knuth: фото, **1931.01.10 Дата рождения**, **07.01.1974 Премия Тьюринга**,
   кнопки **Delete** / **Edit**; справа — результат запроса к Википедии «Knuth Prize»).
8. Примерный вид программного кода **Action-фильтра**.
9. Примерный вид формы для редактирования информации о знаменитости
   (активируется при клике мышью на кнопку **Edit**).
10. Примерный вид форм для подтверждения удаления информации знаменитости.
11. Примерный вид страницы со сведениями о **событиях** знаменитости.
12. Используйте макет (**Layout**) при вёрстке представлений приложения.
13. Используйте **html-helper** для отображения фотографии знаменитости.
14. Продемонстрируйте работоспособность приложения.

## Код Action-фильтра (задание 8) — со скриншота, полностью читаем
```csharp
public class InfoOfCelebrityAsyncActionFilter : Attribute, IAsyncActionFilter
{
    public static readonly string Wikipedia = "WIKI";
    public static readonly string Facebook  = "FACE";
    string infotype;
    public InfoOfCelebrityAsyncActionFilter(string infotype = "") { this.infotype = infotype.ToUpper(); }
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        IRepository repo = context.HttpContext.RequestServices.GetService<IRepository>();
        int id = (int)context.ActionArguments["id"] ?? -1;
        Celebrity? celebrity = repo?.GetCelebrityById(id);
        if (infotype.Contains(Wikipedia) && celebrity != null)
            context.HttpContext.Items.Add(Wikipedia, await WikiInfoCelebrity.GetReferences(celebrity.FullName));
        if (infotype.Contains(Facebook) && celebrity != null)
            context.HttpContext.Items.Add(Facebook, getFromFace(celebrity.FullName));
        await next();
    }
    string getFromFace(string fullname) { return "Info from Face"; }
}
```

### Класс WikiInfoCelebrity (запрос к Википедии) — со скриншота
```csharp
public class WikiInfoCelebrity
{
    HttpClient client;
    //string wikiURItemplate = "https://en.wikipedia.org/w/api.php?action=opensearch&search=\"{0}\"&prop=info&format=json";
    Dictionary<string, string> wikiURIres = new Dictionary<string, string>();
    // this.wikiURI = string.Format("https://en.wikipedia.org/w/api.php?action=opensearch&search\"{0}\"&prop=info&format=json", fullname);
    private WikiInfoCelebrity(string fullname)
    {
        this.client = new HttpClient();
        this.wikiURIres = new Dictionary<string, string>();
        // this.wikiURI = string.Format("https://en.wikipedia.org/w/api.php?action=opensearch&search\"{0}\"&prop=info&format=json", fullname);
    }
    public static async Task<Dictionary<string, string>> GetReferences(string fullname)
    {
        WikiInfoCelebrity info = new WikiInfoCelebrity(fullname);
        HttpResponseMessage message = await info.client.GetAsync(info.wikiURI);
        if (message.StatusCode == System.Net.HttpStatusCode.OK)
        {
            List<dynamic>? result = await message.Content.ReadFromJsonAsync<List<dynamic>>() ?? default(List<dynamic>);
            List<string>? ls1 = JsonSerializer.Deserialize<List<string>>(result[0]);
            List<string>? ls3 = JsonSerializer.Deserialize<List<string>>(result[3]);
            for (int i = 0; i < ls1.Count; i++)
            {
                info.wikiURIres.Add(ls1[i], ls3[i]);
            }
        }
        return info.wikiURIres;
    }
}
```

Ключевые детали фильтра:
- Наследует `Attribute` и реализует `IAsyncActionFilter` — это **action-фильтр** (не resource/result).
- Параметр конструктора `infotype` -> `.ToUpper()`, сравнение через `Contains`, поэтому передаётся
  `"wiki"` / `"facebook"`, а сравнивается с `"WIKI"` / `"FACE"`.
- ID знаменитости берётся из `context.ActionArguments["id"]` — поэтому в маршруте
  параметр назван именно `id`.
- Данные кладутся в `context.HttpContext.Items` -> далее доступны в представлении.
- Метод обработчика — `OnActionExecutionAsync(ActionExecutingContext, ActionExecutionDelegate)`.

## Ключевые API и типы

| Имя | Назначение |
|---|---|
| `AddControllersWithViews()` | подключение MVC с представлениями Razor |
| `MapControllerRoute(name, pattern, defaults)` | маршрутизация MVC |
| `Controller`, `Action`, `ActionResult`, `ViewResult` | контроллер и действия |
| `IAsyncActionFilter`, `ActionExecutingContext`, `ActionExecutionDelegate` | action-фильтры |
| `context.HttpContext.Items` | передача данных из фильтра в представление |
| `context.ActionArguments["id"]` | доступ к параметрам действия |
| `HttpContext.RequestServices.GetService<IRepository>()` | доступ к репозиторию из фильтра |
| `IViewComponentResult` / `_Layout.cshtml`, `_ViewStart.cshtml`, `_ViewImports.cshtml` | Layout MVC |
| `HtmlHelper` / html-helper `CelebrityPhoto` | отображение фотографии (п.13) |
| `HttpClient`, `GetAsync`, `ReadFromJsonAsync<T>()` | запрос к Википедии (opensearch API) |
| `JsonSerializer.Deserialize<List<string>>` | разбор JSON-ответа Википедии |
| `CelebritiesConfig`, `IOptions<CelebritiesConfig>`, `ISO3166Alpha2Path` | конфигурация |
| `MapCelebrities()`, `MapLifeevents()`, `MapPhotoCelebrities()` | методы расширения из библиотеки |
| `UseCelebritiesErrorHandler("ANC28")` | middleware-обработчик ошибок (настраивается префиксом) |

## Ответы для защиты
В ТЗ лабы 8 отдельного раздела «Ответьте на вопросы» нет. Вероятные вопросы (по содержанию
работы и лекций):
- чем MVC отличается от minimal API (лабы 3-6) и от Razor Pages (лаба 7);
- зачем нужен Layout (`_Layout.cshtml`) и как работает `_ViewStart.cshtml`;
- какие типы фильтров есть в ASP.NET Core (action, resource, result, exception, authorization)
  и как выбрать нужный;
- почему `GetService<IRepository>()` возвращает **nullable** (`IRepository?`) и что делать, если сервис не зарегистрирован;
- как работает внедрение зависимостей в контроллере против `RequestServices` в фильтре;
- зачем нужен `ISO3166Alpha2Path` (валидация кода страны по ISO 3166-1 alpha-2).

## Источник
- `labs/Лабораторная_08_ASPNETCORE_MVC_1/Лабораторная_08_ASPNETCORE_MVC_1.pdf` — стр. 1 (шапка,
  задание 1, скриншот шаблона MVC, структура решения ANC28, таблица пояснений),
  стр. 2 (структура проекта, таблица пояснений элементов, «Примерный вид файла Program.cs»),
  стр. 3 (код Program.cs с красными рамками, примерный вид файла конфигурации,
  вид приложения при запуске), стр. 4 (формы ввода и подтверждения),
  стр. 5 (вид формы просмотра знаменитости + результат Википедии, код Action-фильтра и класса
  WikiInfoCelebrity, вид формы редактирования), стр. 6 (вид формы подтверждения удаления),
  стр. 7 (вид страницы событий, п.12 про Layout), стр. 8 (п.14 демонстрация работоспособности).
- Прилагаемый файл `labs/Лабораторная_08_ASPNETCORE_MVC_1/Лабораторная_08__iso3166-1-alpha2-country-codes.json`
  (16 КБ) — справочник кодов стран.