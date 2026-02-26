using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Реализация кэширования в Redis.
/// </summary>
/// <param name="cache">Redis Distributed Cache.</param>
/// <param name="logger">Логгер.</param>
public class RedisCacheService(
    IDistributedCache cache,
    ILogger<RedisCacheService> logger) : ICacheService
{
    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Получение из кэша по ключу {Key}", key);
            var cachedJson = await cache.GetStringAsync(key, cancellationToken);

            if (cachedJson == null)
            {
                logger.LogDebug("Данные по ключу {Key} не найдены", key);
                return default;
            }

            return JsonSerializer.Deserialize<T>(cachedJson);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении данных из кэша по ключу {Key}", key);
            return default;
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Сохранение в кэш по ключу {Key}", key);
            var serialized = JsonSerializer.Serialize(value);

            await cache.SetStringAsync(
                key,
                serialized,
                options ?? new DistributedCacheEntryOptions(),
                cancellationToken);

            logger.LogDebug("Данные сохранены в кэш по ключу {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить данные в кэш по ключу {Key}", key);
        }
    }
}