using CompanyEmployee.Domain.Entity;
using CompanyEmployee.FileService.Services;
using MassTransit;
using System.Text.Json;

namespace CompanyEmployee.FileService.Consumers;

/// <summary>
/// Консьюмер для обработки сообщений о генерации сотрудника.
/// </summary>
public class EmployeeGeneratedConsumer : IConsumer<Employee>
{
    private readonly IS3FileStorage _storage;
    private readonly string _bucketName;
    private readonly ILogger<EmployeeGeneratedConsumer> _logger;

    public EmployeeGeneratedConsumer(IS3FileStorage storage, string bucketName, ILogger<EmployeeGeneratedConsumer> logger)
    {
        _storage = storage;
        _bucketName = bucketName;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<Employee> context)
    {
        var employee = context.Message;
        var fileName = $"employee_{employee.Id}.json";
        var content = JsonSerializer.SerializeToUtf8Bytes(employee);

        await _storage.UploadFileAsync(_bucketName, fileName, content);
        _logger.LogInformation("Сотрудник {Id} сохранён в S3 как {FileName}", employee.Id, fileName);
    }
}