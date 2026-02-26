using CompanyEmployee.Domain.Entity;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Бизнес-логика работы с сотрудниками.
/// </summary>
/// <param name="generator">Генератор сотрудников.</param>
/// <param name="cache">Сервис кэширования.</param>
/// <param name="logger">Логгер.</param>
public class EmployeeService(
    IEmployeeGenerator generator,
    ICacheService cache,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    /// <inheritdoc />
    public async Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"employee:{id}";
        var employee = await cache.GetAsync<Employee>(cacheKey, cancellationToken);
        if (employee != null)
        {
            logger.LogInformation("Сотрудник с ID {Id} найден в кэше", id);
            return employee;
        }

        logger.LogInformation("Сотрудник с ID {Id} не найден в кэше, генерация нового", id);
        employee = generator.Generate(id);

        await cache.SetAsync(cacheKey, employee, _cacheOptions, cancellationToken);

        return employee;
    }
}