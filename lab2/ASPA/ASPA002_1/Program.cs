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
