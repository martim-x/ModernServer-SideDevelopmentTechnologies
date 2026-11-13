using System.Text.Json;

namespace DAL004;

/// <summary>
/// JSON-реализация <see cref="IRepository"/>: данные читаются из файла <see cref="JSONFileName"/>
/// в каталоге <see cref="BasePath"/>, изменения копятся в памяти и записываются методом <see cref="SaveChanges"/>.
/// </summary>
public class Repository : IRepository
{
    /// <summary>Имя JSON-файла с данными. Задаётся приложением до вызова <see cref="Create"/>.</summary>
    public static string JSONFileName { get; set; } = "Celebrities.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string BasePath { get; }

    private Celebrity[] _celebrities = Array.Empty<Celebrity>();

    private Repository(string basePath)
    {
        BasePath = Path.GetFullPath(basePath);
        LoadIfExists();
    }

    /// <summary>Фабричный метод: репозиторий создаётся только через него (конструктор закрыт).</summary>
    public static IRepository Create(string basePath) => new Repository(basePath);

    private string JsonPath => Path.Combine(BasePath, JSONFileName);

    /// <summary>Чтение при старте: отсутствие файла не является ошибкой — работаем с пустым набором.</summary>
    private void LoadIfExists()
    {
        if (!File.Exists(JsonPath))
        {
            _celebrities = Array.Empty<Celebrity>();
            return;
        }

        _celebrities = JsonSerializer.Deserialize<Celebrity[]>(File.ReadAllText(JsonPath), JsonOptions)
                       ?? Array.Empty<Celebrity>();
    }

    /// <summary>Следующий свободный Id: максимум по текущему набору + 1.</summary>
    private int NextId() => _celebrities.Length == 0 ? 1 : _celebrities.Max(celebrity => celebrity.Id) + 1;

    public Celebrity[] getAllCelebrities() => _celebrities;

    public Celebrity? getCelebrityById(int id) =>
        _celebrities.FirstOrDefault(celebrity => celebrity.Id == id);

    public Celebrity[] getCelebritiesBySurname(string surname) =>
        _celebrities
            .Where(celebrity => celebrity.Surname.Equals(surname, StringComparison.OrdinalIgnoreCase))
            .ToArray();

    public string? getPhotoPathById(int id) =>
        _celebrities.FirstOrDefault(celebrity => celebrity.Id == id)?.PhotoPath;

    public int? addCelebrity(Celebrity celebrity)
    {
        int id = NextId();
        _celebrities = _celebrities.Append(celebrity with { Id = id }).ToArray();
        return id;
    }

    public bool delCelebrityById(int id)
    {
        int index = Array.FindIndex(_celebrities, celebrity => celebrity.Id == id);
        if (index < 0)
        {
            return false;
        }

        _celebrities = _celebrities.Where((_, position) => position != index).ToArray();
        return true;
    }

    public int? updCelebrityById(int id, Celebrity celebrity)
    {
        int index = Array.FindIndex(_celebrities, item => item.Id == id);
        if (index < 0)
        {
            return null;
        }

        // Обновление выполняется как «удалить старую запись + добавить новую»:
        // новая запись получает очередной свободный Id (так работает эталонный DAL004 из условия).
        _celebrities = _celebrities.Where((_, position) => position != index).ToArray();
        int newId = NextId();
        _celebrities = _celebrities.Append(celebrity with { Id = newId }).ToArray();
        return newId;
    }

    public int SaveChanges()
    {
        // Файл сначала читается (проверка доступности источника), затем перезаписывается.
        // Если файл удалён во время работы приложения — FileNotFoundException уходит в обработчик ошибок.
        _ = File.ReadAllText(JsonPath);
        File.WriteAllText(JsonPath, JsonSerializer.Serialize(_celebrities, JsonOptions));
        return _celebrities.Length;
    }

    public void Dispose()
    {
        // Неуправляемых ресурсов нет: файл открывается и закрывается внутри Load/SaveChanges.
    }
}
