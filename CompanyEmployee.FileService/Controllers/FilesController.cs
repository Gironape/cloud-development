using System.Text;
using CompanyEmployee.FileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.FileService.Controllers;

/// <summary>
/// Контроллер для работы с файлами сотрудников в MinIO хранилище.
/// </summary>
[ApiController]
[Route("api/files")]
public class FilesController(
    IStorageService storage,
    string bucketName,
    ILogger<FilesController> logger) : ControllerBase
{
    /// <summary>
    /// Получает список всех файлов в бакете.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        logger.LogDebug("Retrieving file list from bucket {BucketName}", bucketName);

        var files = await storage.ListFilesAsync(bucketName);

        logger.LogInformation("Retrieved {Count} files from bucket {BucketName}", files.Count(), bucketName);
        return Ok(new { count = files.Count(), files });
    }

    /// <summary>
    /// Проверяет существование файла в бакете.
    /// </summary>
    [HttpGet("exists/{fileName}")]
    public async Task<IActionResult> FileExists(string fileName)
    {
        logger.LogDebug("Checking existence of file {FileName}", fileName);

        var exists = await storage.FileExistsAsync(bucketName, fileName);

        if (exists)
        {
            logger.LogDebug("File {FileName} exists", fileName);
            return Ok();
        }

        logger.LogDebug("File {FileName} not found", fileName);
        return NotFound();
    }

    /// <summary>
    /// Получает метаданные файла (размер, дата изменения).
    /// </summary>
    [HttpGet("{fileName}/metadata")]
    public async Task<IActionResult> GetFileMetadata(string fileName)
    {
        logger.LogDebug("Retrieving metadata for file {FileName}", fileName);

        var metadata = await storage.GetFileMetadataAsync(bucketName, fileName);
        if (metadata is null)
        {
            logger.LogDebug("Metadata not found for file {FileName}", fileName);
            return NotFound();
        }

        logger.LogInformation("Retrieved metadata for {FileName}: {Size} bytes, modified {LastModified}",
            fileName, metadata.Size, metadata.LastModified);
        return Ok(metadata);
    }

    /// <summary>
    /// Получает содержимое файла сотрудника.
    /// </summary>
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetFile(string fileName)
    {
        logger.LogDebug("Retrieving file {FileName}", fileName);

        var content = await storage.GetFileAsync(bucketName, fileName);
        if (content is null)
        {
            logger.LogDebug("File {FileName} not found", fileName);
            return NotFound();
        }

        var json = Encoding.UTF8.GetString(content);
        logger.LogInformation("File {FileName} retrieved successfully, size {Size} bytes", fileName, content.Length);
        return Content(json, "application/json");
    }
}