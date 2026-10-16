using Microsoft.Extensions.FileProviders;

public class Program
{
    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.UseDefaultFiles(
            new DefaultFilesOptions { DefaultFileNames = new List<string> { "Neumann.html" } }
        );

        app.UseStaticFiles();

        app.UseStaticFiles(
            new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(builder.Environment.ContentRootPath),
                RequestPath = "/static",
            }
        );

        app.Run();
    }
}
