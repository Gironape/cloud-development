using CompanyEmployee.Api.Services;
using CompanyEmployee.Domain.Entity;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.Api.Controllers;

/// <summary>
/// Контроллер для работы с сотрудниками.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService employeeService, ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// Получить сгенерированного сотрудника.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Employee), StatusCodes.Status200OK)]
    public async Task<ActionResult<Employee>> GetEmployee(int? seed, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запрос на получение сотрудника с seed: {Seed}", seed);
        var employee = await _employeeService.GetEmployeeAsync(seed, cancellationToken);
        return Ok(employee);
    }
}