using Bogus;
using Bogus.DataSets;
using CompanyEmployee.Domain.Entity;
using System.Xml.Linq;

namespace CompanyEmployee.Api.Services;

public class EmployeeGenerator : IEmployeeGenerator
{
    private readonly ILogger<EmployeeGenerator> _logger;
    private readonly string[] _professions = { "Developer", "Manager", "Analyst", "Designer", "QA" };
    private readonly string[] _suffixes = { "Junior", "Middle", "Senior" };

    public EmployeeGenerator(ILogger<EmployeeGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Генератор сотрудника
    /// </summary>
    public Employee Generate(int? seed = null)
    {
        if (seed.HasValue)
            Randomizer.Seed = new Random(seed.Value);

        var faker = new Faker();

        var gender = faker.PickRandom<Bogus.DataSets.Name.Gender>();
        var firstName = faker.Name.FirstName(gender);
        var lastName = faker.Name.LastName(gender);
        var patronymicBase = faker.Name.FirstName(Name.Gender.Male);
        var patronymic = gender == Name.Gender.Male
            ? patronymicBase + "ович"
            : patronymicBase + "овна";
        var fullName = $"{lastName} {firstName} {patronymic}";

        var profession = faker.PickRandom(_professions);
        var suffix = faker.PickRandom(_suffixes);
        var position = $"{profession} {suffix}".Trim();

        var department = faker.Commerce.Department();

        var hireDate = DateOnly.FromDateTime(faker.Date.Past(10).ToUniversalTime());

        var salary = suffix switch
        {
            "Junior" => faker.Random.Decimal(30000, 60000),
            "Middle" => faker.Random.Decimal(60000, 100000),
            "Senior" => faker.Random.Decimal(100000, 180000),
            _ => faker.Random.Decimal(40000, 80000)
        };
        salary = Math.Round(salary, 2);

        var email = faker.Internet.Email(firstName, lastName);
        var phone = faker.Phone.PhoneNumber("+7(###)###-##-##");
        var isTerminated = faker.Random.Bool(0.1f);

        DateOnly? terminationDate = null;
        if (isTerminated)
        {
            var termDate = faker.Date.Between(hireDate.ToDateTime(TimeOnly.MinValue), DateTime.Now);
            terminationDate = DateOnly.FromDateTime(termDate);
        }

        var employee = new Employee
        {
            Id = faker.Random.Int(1, 100000),
            FullName = fullName,
            Position = position,
            Department = department,
            HireDate = hireDate,
            Salary = salary,
            Email = email,
            Phone = phone,
            IsTerminated = isTerminated,
            TerminationDate = terminationDate
        };

        _logger.LogInformation("Сгенерирован новый сотрудник: {@Employee}", employee);
        return employee;
    }
}