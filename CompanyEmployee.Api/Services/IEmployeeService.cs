using CompanyEmployee.Domain.Entity;

namespace CompanyEmployee.Api.Services;

public interface IEmployeeService
{
    /// <summary>
    /// Получает сотрудника.
    /// </summary>
    public Task<Employee> GetEmployeeAsync(int? seed, CancellationToken cancellationToken = default);
}