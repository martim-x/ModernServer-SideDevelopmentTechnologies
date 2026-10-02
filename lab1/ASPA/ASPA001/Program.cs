public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddHttpLogging(logging => { });

        var app = builder.Build();

        app.UseHttpLogging();

        app.MapGet("/", () => "Hello World!");

        app.Run();
    }
}