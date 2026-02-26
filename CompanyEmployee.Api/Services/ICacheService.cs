using Microsoft.Extensions.Caching.Distributed;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для работы с распределённым кэшем.
/// </summary>
public interface ICacheService
{
    /// <summary>Получает данные из кэша по ключу.</summary>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Данные из кэша или default.</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>Сохраняет данные в кэш.</summary>
    /// <param name="key">Ключ кэша.</param>
    /// <param name="value">Данные для сохранения.</param>
    /// <param name="options">Опции кэширования.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
}