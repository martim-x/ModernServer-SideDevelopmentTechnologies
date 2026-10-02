// Лабораторная работа 2. Задание 1. Проект ASPA002_1.
//
// Задача 2: к конечной точке /aspnetcore подключается встроенный middleware-элемент
//           UseWelcomePage, отображающий приветственную страницу ASP.NET Core.
// Задача 3: к конечной точке "/" подключается встроенный middleware-элемент
//           UseStaticFiles, обрабатывающий статические файлы каталога wwwroot.
// Задача 5: Index.html становится стартовой страницей благодаря middleware UseDefaultFiles.

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Задача 2. Приветственная страница ASP.NET Core: встроенный middleware UseWelcomePage
// подключается к ветке (branch) конечной точки /aspnetcore.
// Механизм middleware: конвейер запроса - это цепочка обработчиков. Обработчики
// подключаются по порядку; каждый либо самостоятельно формирует ответ, либо вызывает
// await next(context) и передаёт управление следующему обработчику цепочки.
// Первый обработчик, сформировавший ответ, завершает конвейер (POST / GET в ASP.NET Core
// может иметь только один ответ), поэтому порядок подключения принципиален.
app.Map("/aspnetcore", branch => branch.UseWelcomePage());

// Обработчик, расположенный ниже по коду, окажется неработоспособным:
//   app.MapGet("/aspnetcore", () => "Start");
// запрос к /aspnetcore перехватывается раньше: UseWelcomePage сам формирует ответ
// и НЕ вызывает next(), поэтому до конвейера этого MapGet управление не доходит.

// Задача 3 и 5. Статические файлы каталога wwwroot по адресу "/".
// UseDefaultFiles - встроенный middleware: если запрошен каталог, он подставляет
//                   имя стартовой страницы (по умолчанию index.html).
// UseStaticFiles   - встроенный middleware: отдаёт статические файлы (html, css, js, jpg).
// Порядок важен: UseDefaultFiles должен идти перед UseStaticFiles, иначе подстановка
// стартовой страницы не произойдёт.
app.UseDefaultFiles();
app.UseStaticFiles();

app.Run();
