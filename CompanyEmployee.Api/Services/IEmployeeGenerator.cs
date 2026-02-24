using CompanyEmployee.Domain.Entity;

namespace CompanyEmployee.Api.Services;

public interface IEmployeeGenerator
{
    /// <summary>
    /// Генерирует нового сотрудника.
    /// </summary>
    public Employee Generate(int? seed = null);
}