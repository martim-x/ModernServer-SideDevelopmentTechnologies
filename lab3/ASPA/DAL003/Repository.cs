using System.Text.Json;

namespace DAL003;

public class Repository : IRepository
{
    public static string JSONFileName { get; set; } = "Celebrities.json";

    public string BasePath { get; }

    private Celebrity[] _celebrities = Array.Empty<Celebrity>();

    private Repository(string basePath)
    {
        BasePath = Path.GetFullPath(basePath);
        Load();
    }

    public static IRepository Create(string basePath) => new Repository(basePath);

    private void Load()
    {
        var path = Path.Combine(BasePath, JSONFileName);
        if (!File.Exists(path))
        {
            _celebrities = Array.Empty<Celebrity>();
            return;
        }

        var json = File.ReadAllText(path);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _celebrities = JsonSerializer.Deserialize<Celebrity[]>(json, options) ?? Array.Empty<Celebrity>();
    }

    public Celebrity[] getAllCelebrities() => _celebrities;

    public Celebrity? getCelebrityById(int id) =>
        _celebrities.FirstOrDefault(celebrity => celebrity.Id == id);

    public Celebrity[] getCelebritiesBySurname(string Surname) =>
        _celebrities
            .Where(celebrity => celebrity.Surname.Equals(Surname, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public string? getPhotoPathById(int id) =>
        _celebrities.FirstOrDefault(celebrity => celebrity.Id == id)?.PhotoPath;

    public void Dispose()
    {
    }
}

// public Repository(string basePath, string jsonfilename) { BasePath = basePath; var FullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", basePath, jsonfilename); if (File.Exists(FullPath)) { var json = File.ReadAllText(FullPath); _celebrities = JsonSerializer.Deserialize<Celebrity[]>(json); } else { _celebrities = Array.Empty<Celebrity>(); } }
// public static Repository Create(string basepath) { return new Repository(basepath, JSONFileName); }
// public string? getPhotoPathById(int Id) => getCelebrityById(Id)?.PhotoPath;
