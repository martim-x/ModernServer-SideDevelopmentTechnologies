using DAL003;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder();

Repository.JSONFileName = "Celebrities.json";

var celebritiesPath = Path.Combine(builder.Environment.ContentRootPath, "Celebrities");

using (IRepository repository = Repository.Create("Celebrities"))
{
    var app = builder.Build();

    app.MapGet("/Celebrities", () => repository.getAllCelebrities());

    app.MapGet("/Celebrities/{id:int}", (int id) => repository.getCelebrityById(id));

    app.MapGet("/Celebrities/BySurname/{surname}", (string surname) =>
        repository.getCelebritiesBySurname(surname));

    // getPhotoPathById возвращает string?; для отсутствующего Id это null.
    // null из обработчика, возвращающего string, роняет запрос (ArgumentNullException в
    // RequestDelegateFactory), поэтому отсутствующий путь отдаём пустой строкой.
    app.MapGet("/Celebrities/PhotoPathById/{id:int}", (int id) =>
        repository.getPhotoPathById(id) ?? string.Empty);

    app.MapGet("/", () => "Hello World!");

    // Задание 4: фотографии доступны как статические ресурсы по адресу /Photo/...
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(celebritiesPath),
        RequestPath = "/Photo"
    });

    // Задание 5: обзор каталога и скачивание файлов по адресу /Celebrities/download/
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(celebritiesPath),
        RequestPath = "/Celebrities/download",
        OnPrepareResponse = ctx =>
            ctx.Context.Response.Headers.Append(
                "Content-Disposition", $"attachment; filename=\"{ctx.File.Name}\"")
    });

    app.UseDirectoryBrowser(new DirectoryBrowserOptions
    {
        FileProvider = new PhysicalFileProvider(celebritiesPath),
        RequestPath = "/Celebrities/download"
    });

    app.Run();
}

// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();
// string photoDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photo");
// string jsonDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Celebrities");
// var photoProvider = new PhysicalFileProvider(photoDir);
// var jsonProvider = new PhysicalFileProvider(jsonDir);
// var provider = new CompositeFileProvider(photoProvider, jsonProvider);
// app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, RequestPath = "/Photo" });
// app.UseDirectoryBrowser(new DirectoryBrowserOptions { FileProvider = provider, RequestPath = "/Photo" });
// app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, RequestPath = "/Celebrities/download", OnPrepareResponse = ctx => { ctx.Context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{ctx.File.Name}\""); } });
// app.UseDirectoryBrowser(new DirectoryBrowserOptions { FileProvider = provider, RequestPath = "/Celebrities/download" });
// Repository.JSONFileName = "Celebrities.json";
// using (IRepository repository = Repository.Create("Celebrities")) { app.MapGet("/Celebrities", () => repository.getAllCelebrities()); app.MapGet("/Celebrities/{Id:int}", (int Id) => repository.getCelebrityById(Id)); app.MapGet("/Celebrities/BySurname/{surname}", (string surname) => repository.getCelebritiesBySurname(surname)); app.MapGet("/Celebrities/PhotoPathById/{Id:int}", (int Id) => repository.getPhotoPathById(Id)); app.MapGet("/", () => "Hello World!"); }
// app.Run();
