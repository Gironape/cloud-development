using CompanyEmployee.Domain.Entity;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Генератор данных сотрудников.
/// </summary>
public interface IEmployeeGenerator
{
    /// <summary>
    /// Генерирует сотрудника по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор.</param>
    /// <returns>Сгенерированный сотрудник.</returns>
    public Employee Generate(int id);
}