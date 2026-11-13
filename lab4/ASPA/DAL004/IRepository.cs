namespace DAL004;

/// <summary>
/// Контракт доступа к данным (паттерн Repository).
/// Расширен относительно DAL003 операциями изменения: add / del / upd и SaveChanges.
/// </summary>
public interface IRepository : IDisposable
{
    string BasePath { get; }

    Celebrity[] getAllCelebrities();

    Celebrity? getCelebrityById(int id);

    Celebrity[] getCelebritiesBySurname(string Surname);

    string? getPhotoPathById(int id);

    /// <summary>Добавляет запись в память и возвращает присвоенный Id (null — добавить не удалось).</summary>
    int? addCelebrity(Celebrity celebrity);

    /// <summary>Удаляет запись по Id. false — записи с таким Id нет.</summary>
    bool delCelebrityById(int id);

    /// <summary>Изменяет запись по Id. Возвращает новый Id записи (null — записи с таким Id нет).</summary>
    int? updCelebrityById(int id, Celebrity celebrity);

    /// <summary>Записывает текущее состояние в JSON-файл. Возвращает количество сохранённых изменений (0 — изменений не было).</summary>
    int SaveChanges();
}
