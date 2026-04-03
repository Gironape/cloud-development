namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Интерфейс для работы с объектным хранилищем.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Сохраняет файл в хранилище.
    /// </summary>
    public Task SaveFileAsync(string bucketName, string key, byte[] content);

    /// <summary>
    /// Проверяет существование файла в хранилище.
    /// </summary>
    public Task<bool> FileExistsAsync(string bucketName, string key);
}