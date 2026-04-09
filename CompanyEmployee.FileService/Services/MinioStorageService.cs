using Minio;
using Minio.DataModel.Args;

namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Реализация хранилища через MinIO.
/// </summary>
/// <param name="minioClient">Клиент MinIO.</param>
/// <param name="logger">Логгер.</param>
public class MinioStorageService(
    IMinioClient minioClient,
    ILogger<MinioStorageService> logger) : IStorageService
{
    public async Task SaveFileAsync(string bucketName, string key, byte[] content)
    {
        try
        {
            var bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!bucketExists)
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                logger.LogInformation("Создан бакет {BucketName}", bucketName);
            }

            using var stream = new MemoryStream(content);
            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("application/json"));

            logger.LogInformation("Файл {Key} загружен в MinIO", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка загрузки файла {Key}", key);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string key)
    {
        try
        {
            await minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key));
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при проверке файла {Key}", key);
            return false;
        }
    }

    public async Task<IEnumerable<string>> ListFilesAsync(string bucketName)
    {
        var files = new List<string>();
        try
        {
            var args = new ListObjectsArgs()
                .WithBucket(bucketName)
                .WithRecursive(true);

            await foreach (var item in minioClient.ListObjectsEnumAsync(args))
            {
                files.Add(item.Key);
            }

            logger.LogInformation("Получен список файлов из бакета {BucketName}, найдено {Count} файлов", bucketName, files.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении списка файлов из бакета {BucketName}", bucketName);
        }
        return files;
    }

    public async Task<byte[]> GetFileAsync(string bucketName, string key)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await minioClient.GetObjectAsync(args);
            logger.LogInformation("Файл {Key} загружен из MinIO", key);
            return memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении файла {Key}", key);
            return null;
        }
    }

    public async Task<FileMetadata?> GetFileMetadataAsync(string bucketName, string key)
    {
        try
        {
            var args = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key);

            var stat = await minioClient.StatObjectAsync(args);
            return new FileMetadata(key, stat.Size, stat.LastModified);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ошибка при получении метаданных файла {Key}", key);
            return null;
        }
    }
}