using System.Text.Json;
using CompanyEmployee.Domain.Entity;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для работы с сотрудниками.
/// </summary>
/// <param name="generator">Генератор сотрудников.</param>
/// <param name="cache">Кэш Redis.</param>
/// <param name="snsPublisher">Публикатор SNS.</param>
/// <param name="logger">Логгер.</param>
public class EmployeeService(
    IEmployeeGenerator generator,
    IDistributedCache cache,
    SnsPublisherService snsPublisher,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    public async Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"employee:{id}";
        Employee? employee = null;

        try
        {
            var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (cachedJson != null)
            {
                logger.LogInformation("Сотрудник с ID {Id} найден в кэше", id);
                employee = JsonSerializer.Deserialize<Employee>(cachedJson);
                return employee;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении данных из кэша");
        }

        try
        {
            logger.LogInformation("Сотрудник с ID {Id} не найден в кэше, генерация нового", id);
            employee = generator.Generate(id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при генерации сотрудника с ID {Id}", id);
            return null;
        }

        try
        {
            var serialized = JsonSerializer.Serialize(employee);
            await cache.SetStringAsync(cacheKey, serialized, _cacheOptions, cancellationToken);
            logger.LogDebug("Сотрудник с ID {Id} сохранён в кэш", id);

            await snsPublisher.PublishEmployeeAsync(employee, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось отправить сотрудника {Id} в SNS", id);
        }

        return employee;
    }
}