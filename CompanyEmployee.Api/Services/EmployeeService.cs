using System.Text.Json;
using CompanyEmployee.Domain.Entity;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для работы с сотрудниками, включая кэширование и публикацию в SNS.
/// </summary>
public class EmployeeService(
    IEmployeeGenerator generator,
    IDistributedCache cache,
    SnsPublisherService snsPublisher,
    ILogger<EmployeeService> logger) : IEmployeeService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly DistributedCacheEntryOptions _cacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    /// <inheritdoc/>
    public async Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"employee:{id}";

        var cachedEmployee = await TryGetFromCacheAsync(cacheKey, id, cancellationToken);
        if (cachedEmployee is not null)
        {
            return cachedEmployee;
        }

        var employee = GenerateEmployee(id);
        if (employee is null)
        {
            return null;
        }

        await SaveToCacheAsync(cacheKey, employee, id, cancellationToken);
        await PublishToSnsAsync(employee, id, cancellationToken);

        return employee;
    }

    private async Task<Employee?> TryGetFromCacheAsync(string cacheKey, int id, CancellationToken cancellationToken)
    {
        try
        {
            var cachedJson = await cache.GetStringAsync(cacheKey, cancellationToken);
            if (!string.IsNullOrEmpty(cachedJson))
            {
                logger.LogDebug("Employee {EmployeeId} found in cache", id);
                return JsonSerializer.Deserialize<Employee>(cachedJson, _jsonOptions);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve employee {EmployeeId} from cache", id);
        }

        return null;
    }

    private Employee? GenerateEmployee(int id)
    {
        try
        {
            logger.LogDebug("Employee {EmployeeId} not found in cache, generating new employee", id);
            var employee = generator.Generate(id);
            logger.LogInformation("Generated employee {EmployeeId}: {FullName}", id, employee.FullName);
            return employee;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate employee {EmployeeId}", id);
            return null;
        }
    }

    private async Task SaveToCacheAsync(string cacheKey, Employee employee, int id, CancellationToken cancellationToken)
    {
        try
        {
            var serialized = JsonSerializer.Serialize(employee, _jsonOptions);
            await cache.SetStringAsync(cacheKey, serialized, _cacheOptions, cancellationToken);
            logger.LogDebug("Employee {EmployeeId} saved to cache", id);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save employee {EmployeeId} to cache", id);
        }
    }

    private async Task PublishToSnsAsync(Employee employee, int id, CancellationToken cancellationToken)
    {
        try
        {
            await snsPublisher.PublishEmployeeAsync(employee, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to publish employee {EmployeeId} to SNS", id);
        }
    }
}