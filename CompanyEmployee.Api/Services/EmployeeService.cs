using System.Text.Json;
using CompanyEmployee.Domain.Entity;
using Microsoft.Extensions.Caching.Distributed;
using MassTransit;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для работы с сотрудниками.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeGenerator _generator;
    private readonly IDistributedCache _cache;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<EmployeeService> _logger;
    private readonly DistributedCacheEntryOptions _cacheOptions;

    public EmployeeService(
        IEmployeeGenerator generator,
        IDistributedCache cache,
        IPublishEndpoint publishEndpoint,
        ILogger<EmployeeService> logger)
    {
        _generator = generator;
        _cache = cache;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
    }

    public async Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"employee:{id}";
        Employee? employee = null;

        try
        {
            var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
            if (cachedJson != null)
            {
                _logger.LogInformation("Сотрудник с ID {Id} найден в кэше", id);
                employee = JsonSerializer.Deserialize<Employee>(cachedJson);
                return employee;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при получении данных из кэша");
        }

        try
        {
            _logger.LogInformation("Сотрудник с ID {Id} не найден в кэше, генерация нового", id);
            employee = _generator.Generate(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации сотрудника с ID {Id}", id);
            return null;
        }

        try
        {
            var serialized = JsonSerializer.Serialize(employee);
            await _cache.SetStringAsync(cacheKey, serialized, _cacheOptions, cancellationToken);
            _logger.LogDebug("Сотрудник с ID {Id} сохранён в кэш", id);

            await _publishEndpoint.Publish(employee, cancellationToken);
            _logger.LogInformation("Сообщение о сотруднике {Id} отправлено в SNS", id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не удалось отправить сообщение о сотруднике {Id}", id);
        }

        return employee;
    }
}