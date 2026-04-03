using Amazon.S3;
using Amazon.S3.Model;

namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Реализация S3 хранилища.
/// </summary>
public class S3FileStorage : IS3FileStorage
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3FileStorage> _logger;

    public S3FileStorage(IAmazonS3 s3Client, ILogger<S3FileStorage> logger)
    {
        _s3Client = s3Client;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task EnsureBucketExistsAsync(string bucketName)
    {
        try
        {
            var bucketExists = await _s3Client.DoesS3BucketExistAsync(bucketName);
            if (!bucketExists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
                _logger.LogInformation("Создан бакет {BucketName}", bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при создании бакета {BucketName}", bucketName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UploadFileAsync(string bucketName, string key, byte[] content)
    {
        try
        {
            await EnsureBucketExistsAsync(bucketName);

            using var stream = new MemoryStream(content);
            await _s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = bucketName,
                Key = key,
                InputStream = stream,
                ContentType = "application/json"
            });

            _logger.LogInformation("Файл {Key} загружен в {BucketName}", key, bucketName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка загрузки файла {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> FileExistsAsync(string bucketName, string key)
    {
        try
        {
            await _s3Client.GetObjectMetadataAsync(bucketName, key);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке файла {Key}", key);
            return false;
        }
    }
}