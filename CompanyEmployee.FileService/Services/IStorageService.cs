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
    /// Проверяет существование файла.
    /// </summary>
    public Task<bool> FileExistsAsync(string bucketName, string key);

    /// <summary>
    /// Возвращает список всех файлов в бакете.
    /// </summary>
    public Task<IEnumerable<string>> ListFilesAsync(string bucketName);

    /// <summary>
    /// Возвращает содержимое файла.
    /// </summary>
    public Task<byte[]> GetFileAsync(string bucketName, string key);

    /// <summary>
    /// Возвращает метаданные файла.
    /// </summary>
    public Task<FileMetadata?> GetFileMetadataAsync(string bucketName, string key);
}

/// <summary>
/// Метаданные файла.
/// </summary>
public record FileMetadata(string Name, long Size, DateTime LastModified);