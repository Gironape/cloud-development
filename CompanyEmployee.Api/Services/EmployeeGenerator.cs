using Bogus;
using Bogus.DataSets;
using CompanyEmployee.Domain.Entity;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Генератор сотрудников.
/// </summary>
/// <param name="logger">Логгер.</param>
public class EmployeeGenerator(ILogger<EmployeeGenerator> logger) : IEmployeeGenerator
{
    private readonly string[] _professions = { "Developer", "Manager", "Analyst", "Designer", "QA" };
    private readonly string[] _suffixes = { "Junior", "Middle", "Senior" };

    /// <inheritdoc />
    public Employee Generate(int id)
    {
        Randomizer.Seed = new Random(id);
        var faker = new Faker("ru");
        var employee = new Faker<Employee>("ru")
            .RuleFor(e => e.Id, id)
            .RuleFor(e => e.FullName, f =>
            {
                var gender = f.PickRandom<Name.Gender>();
                var firstName = f.Name.FirstName(gender);
                var lastName = f.Name.LastName(gender);
                var fatherName = f.Name.FirstName(Name.Gender.Male);
                var patronymic = gender == Name.Gender.Male
                    ? fatherName.EndsWith("й") || fatherName.EndsWith("ь")
                        ? fatherName[..^1] + "евич"
                        : fatherName + "ович"
                    : fatherName.EndsWith("й") || fatherName.EndsWith("ь")
                        ? fatherName[..^1] + "евна"
                        : fatherName + "овна";

                return $"{lastName} {firstName} {patronymic}";
            })
            .RuleFor(e => e.Position, f =>
            {
                var profession = f.PickRandom(_professions);
                var suffix = f.PickRandom(_suffixes);
                return $"{profession} {suffix}";
            })
            .RuleFor(e => e.Department, f => f.Commerce.Department())
            .RuleFor(e => e.HireDate, f =>
                DateOnly.FromDateTime(f.Date.Past(10).ToUniversalTime()))
            .RuleFor(e => e.Salary, f =>
            {
                var suffix = f.PickRandom(_suffixes);
                var salary = suffix switch
                {
                    "Junior" => f.Random.Decimal(30000, 60000),
                    "Middle" => f.Random.Decimal(60000, 100000),
                    "Senior" => f.Random.Decimal(100000, 180000),
                    _ => f.Random.Decimal(40000, 80000)
                };
                return Math.Round(salary, 2);
            })
            .RuleFor(e => e.Email, (f, e) =>
            {
                var nameParts = e.FullName.Split(' ');
                return f.Internet.Email(nameParts[1], nameParts[0], "company.ru");
            })
            .RuleFor(e => e.Phone, f => f.Phone.PhoneNumber("+7(###)###-##-##"))
            .RuleFor(e => e.IsTerminated, f => f.Random.Bool(0.1f))
            .RuleFor(e => e.TerminationDate, (f, e) =>
                e.IsTerminated
                    ? DateOnly.FromDateTime(f.Date.Between(
                        e.HireDate.ToDateTime(TimeOnly.MinValue),
                        DateTime.Now))
                    : null)
            .Generate();

        logger.LogInformation("Сгенерирован сотрудник ID {Id}: {FullName}", employee.Id, employee.FullName);
        return employee;
    }
}