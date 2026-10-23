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

// var builder = WebApplication.CreateBuilder(args);
// var app = builder.Build();
// DefaultFilesOptions options = new DefaultFilesOptions();
// options.DefaultFileNames.Clear();
// options.DefaultFileNames.Add("Neumann.html");
// app.UseDefaultFiles(options);
// app.UseStaticFiles();
// app.UseStaticFiles("/static");
// app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Picture")), RequestPath = "/Picture" });
// app.Run();
