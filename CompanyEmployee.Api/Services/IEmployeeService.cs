using CompanyEmployee.Domain.Entity;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для работы с сотрудниками.
/// </summary>
public interface IEmployeeService
{
    /// <summary>
    /// Получает сотрудника по идентификатору с использованием кэширования.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Сотрудник или null.</returns>
    public Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken = default);
}