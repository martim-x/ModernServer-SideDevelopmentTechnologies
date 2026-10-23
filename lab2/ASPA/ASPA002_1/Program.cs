public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.Map("/aspnetcore", branch => branch.UseWelcomePage());

        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.Run();
    }
}

// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();
// app.UseDefaultFiles();
// app.UseWelcomePage("/aspnetcore");
// app.UseStaticFiles();
// app.MapGet("/aspnetcore", () => "Hello World!");
// app.Run();
