using CompanyEmployee.Domain.Entity;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Реализация сервиса сотрудников с кэшированием в Redis через IDistributedCache.
/// </summary>
public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeGenerator _generator;
    private readonly IDistributedCache _cache;
    private readonly ILogger<EmployeeService> _logger;
    private readonly DistributedCacheEntryOptions _cacheOptions;

    public EmployeeService(IEmployeeGenerator generator, IDistributedCache cache, ILogger<EmployeeService> logger)
    {
        _generator = generator;
        _cache = cache;
        _logger = logger;
        _cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
    }

    /// <summary>
    /// Получает сотрудника.
    /// </summary>
    public async Task<Employee> GetEmployeeAsync(int? seed, CancellationToken cancellationToken = default)
    {
        var cacheKey = seed.HasValue ? $"employee:seed:{seed}" : $"employee:random:{Guid.NewGuid()}";

        _logger.LogDebug("Попытка получения сотрудника из кэша по ключу {CacheKey}", cacheKey);

        var cachedJson = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cachedJson != null)
        {
            _logger.LogInformation("Сотрудник найден в кэше по ключу {CacheKey}", cacheKey);
            var employee = JsonSerializer.Deserialize<Employee>(cachedJson);
            return employee!;
        }

        _logger.LogInformation("Сотрудник не найден в кэше, генерация нового. Seed: {Seed}", seed);
        var newEmployee = _generator.Generate(seed);

        var serialized = JsonSerializer.Serialize(newEmployee);
        await _cache.SetStringAsync(cacheKey, serialized, _cacheOptions, cancellationToken);
        _logger.LogDebug("Сгенерированный сотрудник сохранён в кэш по ключу {CacheKey}", cacheKey);

        return newEmployee;
    }
}