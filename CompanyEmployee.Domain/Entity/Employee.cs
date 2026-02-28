namespace CompanyEmployee.Domain.Entity;

/// <summary>
/// Представляет сотрудника компании со всеми характеристиками.
/// </summary>
public class Employee
{
    /// <summary>
    /// Идентификатор сотрудника в системе
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// ФИО
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Должность
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Отдел
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Дата приема
    /// </summary>
    public DateOnly HireDate { get; set; }

    /// <summary>
    /// Зарплата
    /// </summary>
    public decimal Salary { get; set; }

    /// <summary>
    /// Электронная почта
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Телефон
    /// </summary>
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Индикатор увольнения
    /// </summary>
    public bool IsTerminated { get; set; }

    /// <summary>
    /// Дата увольнения
    /// </summary>
    public DateOnly? TerminationDate { get; set; }
}