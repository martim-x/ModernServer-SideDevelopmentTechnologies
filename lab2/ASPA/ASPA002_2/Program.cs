// Лабораторная работа 2. Задание 2. Проект ASPA002_2.
//
// Приложение аналогично ASPA002_1, но:
//   9.1) стартовой страницей назначена Neumann.html (изменён список имён стартовых страниц);
//   9.2) к конечной точке "/static" подключён встроенный middleware-элемент UseStaticFiles;
//   9.3) создан дополнительный каталог статических файлов Picture (рядом с wwwroot),
//        в него перенесён файл Neumann.jpg;
//   9.4) в Neumann.html исправлен путь к фотографии.

using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// 9.1. Стартовая страница Neumann.html.
// Свойство DefaultFileNames - это и есть список имён стартовых страниц.
// По умолчанию здесь указаны index.html, default.html, default.htm.
// Порядок middleware важен: UseDefaultFiles должен идти перед UseStaticFiles.
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "Neumann.html" }
});

// Статические файлы стандартного каталога wwwroot по адресу "/".
app.UseStaticFiles();

// 9.2-9.3. Дополнительный каталог статических файлов Picture, подключённый к "/static".
// PhysicalFileProvider задаёт физический каталог, RequestPath - адрес, по которому
// он публикуется. Корень провайдера - каталог проекта, поэтому файл из дополнительного
// каталога Picture доступен по адресу /static/Picture/Neumann.jpg.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
    RequestPath = "/static"
});

app.Run();
