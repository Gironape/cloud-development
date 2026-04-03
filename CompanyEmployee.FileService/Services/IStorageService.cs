namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Сервис для работы с объектным хранилищем.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Сохраняет файл в хранилище.
    /// </summary>
    public Task SaveFileAsync(string key, byte[] content, string contentType = "application/json");

    /// <summary>
    /// Получает файл из хранилища.
    /// </summary>
    public Task<byte[]> GetFileAsync(string key);

    /// <summary>
    /// Проверяет существование файла.
    /// </summary>
    public Task<bool> FileExistsAsync(string key);
}