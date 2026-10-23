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

    app.MapGet("/Celebrities/PhotoPathById/{id:int}", (int id) =>
        repository.getPhotoPathById(id));

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
        RequestPath = "/Celebrities/download"
    });

    app.UseDirectoryBrowser(new DirectoryBrowserOptions
    {
        FileProvider = new PhysicalFileProvider(celebritiesPath),
        RequestPath = "/Celebrities/download"
    });

    app.Run();
}
