using Minio;
using Minio.DataModel.Args;

namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Реализация хранилища через MinIO.
/// </summary>
public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;

    public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }

    public async Task SaveFileAsync(string bucketName, string key, byte[] content)
    {
        try
        {
            var bucketExists = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!bucketExists)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                _logger.LogInformation("Создан бакет {BucketName}", bucketName);
            }

            using var stream = new MemoryStream(content);
            await _minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("application/json"));

            _logger.LogInformation("Файл {Key} загружен в MinIO", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки файла {Key}", key);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string bucketName, string key)
    {
        try
        {
            await _minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key));
            return true;
        }
        catch
        {
            return false;
        }
    }
}