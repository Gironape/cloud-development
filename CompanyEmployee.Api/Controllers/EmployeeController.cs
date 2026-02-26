using CompanyEmployee.Api.Services;
using CompanyEmployee.Domain.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.Api.Controllers;

/// <summary>
/// Контроллер для работы с сотрудниками.
/// </summary>
/// <param name="employeeService">Сервис для получения сотрудников с кэшированием.</param>
/// <param name="logger">Логгер для записи информации о запросах.</param>
[ApiController]
[Route("api/[controller]")]
public class EmployeeController(
    IEmployeeService employeeService,
    ILogger<EmployeeController> logger) : ControllerBase
{
    /// <summary>
    /// Получить сотрудника по идентификатору.
    /// </summary>
    /// <param name="id">Идентификатор сотрудника.</param>
    /// <param name="cancellationToken">Токен отмены операции.</param>
    /// <returns>Объект сотрудника.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Employee), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<Employee>> GetEmployee(int id, CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Запрос на получение сотрудника с id: {Id}", id);

            if (id <= 0)
            {
                return BadRequest("ID должен быть положительным числом");
            }

            var employee = await employeeService.GetEmployeeAsync(id, cancellationToken);

            if (employee == null)
            {
                return NotFound($"Сотрудник с ID {id} не найден");
            }

            return Ok(employee);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при получении сотрудника с id: {Id}", id);
            return StatusCode(500, "Внутренняя ошибка сервера");
        }
    }
}