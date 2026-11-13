using DAL004;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

Repository.JSONFileName = "Celebrities.json";

const string CelebrityRoute = "/Celebrities";

using (IRepository repository = Repository.Create("Celebrities"))
{
    var app = builder.Build();

    // Любое необработанное исключение перехватывается и обрабатывается конечной точкой /Celebrities/Error
    app.UseExceptionHandler("/Celebrities/Error");

    app.MapGet("/Celebrities", () => repository.getAllCelebrities());

    app.MapGet("/Celebrities/{id:int}", (int id) =>
    {
        Celebrity? celebrity = repository.getCelebrityById(id);
        if (celebrity == null) throw new FoundByIdException($"{CelebrityRoute}, Celebrity Id = {id}");
        return celebrity;
    });

    app.MapPost("/Celebrities", (Celebrity celebrity) =>
    {
        int? id = repository.addCelebrity(celebrity);
        if (id == null) throw new AddCelebrityException("/Celebrities error, id == null");
        if (repository.SaveChanges() <= 0) throw new SaveException("/Celebrities error, SaveChanges() <= 0");
        return new Celebrity((int)id, celebrity.Firstname, celebrity.Surname, celebrity.PhotoPath);
    });

    // Обработка DELETE-запроса
    app.MapDelete("/Celebrities/{id:int}", (int id) =>
    {
        if (!repository.delCelebrityById(id)) throw new DeleteByIdException($"{CelebrityRoute} error, Id = {id}");
        if (repository.SaveChanges() <= 0) throw new SaveException($"{CelebrityRoute} error, SaveChanges() <= 0");
        return Results.Json($"Celebrity with Id = {id} deleted");
    });

    // Задание 4, п.31: обработка PUT-запроса
    app.MapPut("/Celebrities/{id:int}", (int id, Celebrity celebrity) =>
    {
        int? newId = repository.updCelebrityById(id, celebrity);
        if (newId == null) throw new UpdateByIdException($"{CelebrityRoute} error, Id = {id}");
        if (repository.SaveChanges() <= 0) throw new SaveException($"{CelebrityRoute} error, SaveChanges() <= 0");
        return new Celebrity((int)newId, celebrity.Firstname, celebrity.Surname, celebrity.PhotoPath);
    });

    // Запрос, не совпавший ни с одной конечной точкой
    app.MapFallback((HttpContext ctx) => Results.NotFound(new { error = $"path {ctx.Request.Path} not supported" }));

    app.Map("/Celebrities/Error", (HttpContext ctx) =>
    {
        Exception? ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
        IResult rc = Results.Problem(detail: "Panic", instance: app.Environment.EnvironmentName,
            title: "ASPA004", statusCode: 500);
        if (ex != null)
        {
            if (ex is FoundByIdException) rc = Results.NotFound(ex.Message);                        // не найден
            if (ex is DeleteByIdException) rc = Results.NotFound(ex.Message);                       // не найден при удалении
            if (ex is UpdateByIdException) rc = Results.NotFound(ex.Message);                       // не найден при изменении
            if (ex is BadHttpRequestException) rc = Results.BadRequest(ex.Message);                // ошибка в формате запроса
            if (ex is SaveException) rc = Results.Problem(title: "ASPA004/SaveChanges",
                detail: ex.Message, instance: app.Environment.EnvironmentName, statusCode: 500);
            if (ex is AddCelebrityException) rc = Results.Problem(title: "ASPA004/addCelebrity",
                detail: ex.Message, instance: app.Environment.EnvironmentName, statusCode: 500);
            if (ex is FileNotFoundException) rc = Results.Problem(title: "ASPA004",
                detail: ex.Message, instance: app.Environment.EnvironmentName, statusCode: 500);
        }
        return rc;
    });

    app.Run();
}

public class FoundByIdException : Exception
{
    public FoundByIdException(string message) : base($"Found by Id: {message}") { }
}

public class DeleteByIdException : Exception
{
    public DeleteByIdException(string message) : base($"Delete by Id:DELETE {message}") { }
}

public class UpdateByIdException : Exception
{
    public UpdateByIdException(string message) : base($"Update by Id:PUT {message}") { }
}

public class SaveException : Exception
{
    public SaveException(string message) : base($"SaveChanges error: {message}") { }
}

public class AddCelebrityException : Exception
{
    public AddCelebrityException(string message) : base($"AddCelebrityException error: {message}") { }
}
