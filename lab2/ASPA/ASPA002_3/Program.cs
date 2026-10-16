using Microsoft.AspNetCore.Diagnostics;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        // Отключаем служебные сообщения диагностики, чтобы в консоли
        // остались только сообщения приложения (ExceptionHandler).
        builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics", LogLevel.None);

        var app = builder.Build();

        // Задание 3: обработчик исключений должен стоять ДО endpoint'ов,
        // чтобы перехватывать все исключения в конвейере.
        app.UseExceptionHandler("/error");

        // 5 конечных точек
        app.MapGet("/", () => "Start");

        // /test1 — пользовательское Exception
        app.MapGet(
            "/test1",
            () =>
            {
                throw new Exception("-- Exception Test --");
            }
        );

        // /test2 — DivideByZeroException (деление на 0)
        app.MapGet(
            "/test2",
            () =>
            {
                int x = 0;
                int y = 5;
                int z = y / x; // деление на ноль → DivideByZeroException
                return "test2";
            }
        );

        // /test3 — IndexOutOfRangeException (выход за границы массива)
        app.MapGet(
            "/test3",
            () =>
            {
                int[] x = new int[3] { 1, 2, 3 };
                int y = x[3]; // индекс 3 вне диапазона [0..2] → IndexOutOfRangeException
                return "test3";
            }
        );

        // /error — обработчик исключений с выводом страницы <h1>Oops!</h1>
        // и записью в журнал через ILogger<Program>.
        app.Map(
            "/error",
            async (ILogger<Program> logger, HttpContext context) =>
            {
                IExceptionHandlerFeature? exobj = context.Features.Get<IExceptionHandlerFeature>();
                await context.Response.WriteAsync("<h1>Oops!</h1>");
                logger.LogError(exobj?.Error, "ExceptionHandler");
            }
        );

        app.Run();
    }
}
