# Современные технологии серверной разработки

Учебный репозиторий по дисциплине «Современные технологии серверной разработки» (ИСиТ, 3 курс, осенний семестр 2026/2027).

| | |
|---|---|
| Преподаватель | доц. Смелов В.В. |
| Лекция | пт 9.35–11.00 |
| Лабораторная | сб 13.00–14.25, ауд. 322-1 |
| Стек | ASP.NET Core (minimal API, middleware, MVC), .NET 8 |

## Структура

| Каталог | Содержимое |
|---|---|
| `lectures/` | Условия лекций (PDF) + рабочие заметки `.md` с извлечённой информацией |
| `labs/` | Условия лабораторных работ (PDF) |
| `labN/` | Решения и рабочие материалы по конкретным ЛР |

### `labs/` — условия лабораторных работ

Лабы, у которых **больше одного файла**, вынесены в собственный каталог (файл условия + прилагаемые ресурсы). Лабы с одним файлом лежат плоско.

| Лаба | Тема | Что внутри |
|---|---|---|
| 01 | HelloWorld | ASP.NET Core, первое приложение |
| 02 | Static & ErrorHandler | `UseWelcomePage`, `UseStaticFiles`, `UseDefaultFiles`, `UseExceptionHandler` |
| 03 | Repository | паттерн Repository, DAL на JSON, `IDisposable`, `UseDirectoryBrowser` |
| 04 | Map / API | `MapGet`/`MapPost`/`MapPut`/`MapDelete`, CRUD, Postman, Problem Details |
| 05 | Routing | endpoint-фильтры, `AddEndpointFilter<>`, констрейнты маршрутов |
| 06 | API | **каталог** — БД «Life Events of Celebrities», `DAL_Celebrity_MSSQL`, `IOptions<CelebritiesConfig>`, middleware-обработчик ошибок, `Index.html` |
| 07 | Page | формы ввода/подтверждения, работа с БД, фотографии |
| 08 | MVC | **каталог** — контроллеры, action-фильтры, Html-helper, Layout, частичные представления, ISO-коды стран (`iso3166-1-alpha2-country-codes.json`) |

### `lectures/` — лекции

| Файл | Тема |
|---|---|
| `Лекция_00_ASPNETCORE_Интернет.pdf` | Интернет, клиент-серверная архитектура, HTTP |
| `Лекция_01_ASPNETCORE_Введение.pdf` | Введение в ASP.NET Core |
| `Лекция_05_ASPNETCORE_VALIDATION.pdf` | DataAnnotations, FluentValidation, RFC 7807/9457 |
| `Лекция_06_ASPNETCORE_MIDDLEWARE_ERRORHANDLER.pdf` | Middleware, конвейер, обработка ошибок |

## Соглашения по именованию

- Условия лаб: `Лабораторная_NN_ASPNETCORE_<тема>.pdf`
- Условия лекций: `Лекция_NN_ASPNETCORE_<тема>.pdf`
- Рабочие заметки лежат рядом с условием, с тем же именем и расширением `.md`. Сейчас есть у лекций и у лаб 06 и 08 — остальные заметки удалены как невостребованные
- Исходные `.docx` удалены: условия хранятся в PDF (единый формат, текстовый слой)

## Что где искать

- Код решений — в `labN/` (внутри `labN/ASPA/` — файл решения `.sln` и проекты)
- Теория по лабе — `labN/labN_теория.md`
- Ответы на вопросы к лабе — `labN/labN_вопросы_ответы.md`
- Заметки по теме из условия — `labs/Лабораторная_NN_*.md`

## Сборка и запуск

```bash
cd lab2/ASPA
dotnet build
dotnet run --project ASPA002_1
```

Подробности по каждой лабе — в её рабочих заметках.
