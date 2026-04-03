namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Интерфейс для работы с S3 хранилищем.
/// </summary>
public interface IS3FileStorage
{
    /// <summary>
    /// Проверяет существование бакета и создаёт его при необходимости.
    /// </summary>
    /// <param name="bucketName">Имя бакета.</param>
    public Task EnsureBucketExistsAsync(string bucketName);

    /// <summary>
    /// Загружает файл в хранилище.
    /// </summary>
    /// <param name="bucketName">Имя бакета.</param>
    /// <param name="key">Ключ (имя файла).</param>
    /// <param name="content">Содержимое файла.</param>
    public Task UploadFileAsync(string bucketName, string key, byte[] content);

    /// <summary>
    /// Проверяет существование файла в хранилище.
    /// </summary>
    /// <param name="bucketName">Имя бакета.</param>
    /// <param name="key">Ключ (имя файла).</param>
    /// <returns>True если файл существует, иначе False.</returns>
    public Task<bool> FileExistsAsync(string bucketName, string key);
}