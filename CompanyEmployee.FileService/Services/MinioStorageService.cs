using Minio;
using Minio.DataModel.Args;

namespace CompanyEmployee.FileService.Services;

/// <summary>
/// Реализация хранилища через MinIO с использованием первичного конструктора.
/// </summary>
public class MinioStorageService(
    IMinioClient minioClient,
    ILogger<MinioStorageService> logger) : IStorageService
{
    /// <inheritdoc/>
    public async Task SaveFileAsync(string bucketName, string key, byte[] content)
    {
        try
        {
            var bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName));
            if (!bucketExists)
            {
                await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucketName));
                logger.LogInformation("Created bucket {BucketName}", bucketName);
            }

            using var stream = new MemoryStream(content);
            await minioClient.PutObjectAsync(new PutObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("application/json"));

            logger.LogInformation("File {Key} uploaded to MinIO successfully", key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upload file {Key} to bucket {BucketName}", key, bucketName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> FileExistsAsync(string bucketName, string key)
    {
        try
        {
            await minioClient.StatObjectAsync(new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key));
            return true;
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            logger.LogDebug("File {Key} not found in bucket {BucketName}", key, bucketName);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error checking file existence for {Key} in bucket {BucketName}", key, bucketName);
            return false;
        }
    }

    /// <inheritdoc/>
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

            logger.LogInformation("Retrieved {Count} files from bucket {BucketName}", files.Count, bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list files from bucket {BucketName}", bucketName);
        }
        return files;
    }

    /// <inheritdoc/>
    public async Task<byte[]?> GetFileAsync(string bucketName, string key)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            var args = new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(key)
                .WithCallbackStream(stream => stream.CopyTo(memoryStream));

            await minioClient.GetObjectAsync(args);
            logger.LogInformation("File {Key} downloaded from MinIO successfully", key);
            return memoryStream.ToArray();
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            logger.LogWarning("File {Key} not found in bucket {BucketName}", key, bucketName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download file {Key} from bucket {BucketName}", key, bucketName);
            return null;
        }
    }

    /// <inheritdoc/>
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
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            logger.LogDebug("File metadata not found for {Key} in bucket {BucketName}", key, bucketName);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to get metadata for file {Key} in bucket {BucketName}", key, bucketName);
            return null;
        }
    }
}