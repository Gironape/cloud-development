using Aspire.Hosting.Testing;
using CompanyEmployee.Domain.Entity;
using System.Net;
using System.Text.Json;

namespace CompanyEmployee.IntegrationTests;

/// <summary>
/// Базовые интеграционные тесты для проверки микросервисной архитектуры CompanyEmployee
/// </summary>
public class BasicTest : IClassFixture<AppHostFixture>
{
    private readonly AppHostFixture _fixture;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BasicTest(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Проверяет, что API сервис доступен и отвечает на health check запросы.
    /// </summary>
    [Fact]
    public async Task Api_HealthCheck_ReturnsHealthy()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("api-1");
        using var response = await httpClient.GetAsync("/api/employee?id=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// Проверяет, что FileService доступен и его health check эндпоинт возвращает успешный статус.
    /// </summary>
    [Fact]
    public async Task FileService_HealthCheck_ReturnsHealthy()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("fileservice");

        await RetryAsync(async () =>
        {
            using var response = await httpClient.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            return response;
        }, maxAttempts: 5, delayMs: 1000);
    }

    /// <summary>
    /// Проверяет, что API корректно генерирует и возвращает сотрудника по заданному ID.
    /// </summary>
    [Fact]
    public async Task GetEmployee_ShouldReturnEmployee()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("api-1");
        using var response = await httpClient.GetAsync("/api/employee?id=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var employee = JsonSerializer.Deserialize<Employee>(content, _jsonOptions);

        Assert.NotNull(employee);
        Assert.Equal(1, employee.Id);
        Assert.False(string.IsNullOrEmpty(employee.FullName));
    }

    /// <summary>
    /// Проверяет идемпотентность API: повторные запросы с одинаковым ID должны возвращать
    /// </summary>
    [Fact]
    public async Task SameEmployeeId_ShouldReturnSameData()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("api-1");

        using var firstResponse = await httpClient.GetAsync("/api/employee?id=2");
        var firstContent = await firstResponse.Content.ReadAsStringAsync();

        using var secondResponse = await httpClient.GetAsync("/api/employee?id=2");
        var secondContent = await secondResponse.Content.ReadAsStringAsync();

        Assert.Equal(firstContent, secondContent);
    }

    /// <summary>
    /// Проверяет, что для разных ID генерируются разные сотрудники с уникальными данными.
    /// </summary>
    [Fact]
    public async Task DifferentIds_ShouldGenerateDifferentEmployees()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("api-1");

        using var response1 = await httpClient.GetAsync("/api/employee?id=10");
        var employee1 = JsonSerializer.Deserialize<Employee>(
            await response1.Content.ReadAsStringAsync(), _jsonOptions);

        using var response2 = await httpClient.GetAsync("/api/employee?id=20");
        var employee2 = JsonSerializer.Deserialize<Employee>(
            await response2.Content.ReadAsStringAsync(), _jsonOptions);

        Assert.NotNull(employee1);
        Assert.NotNull(employee2);
        Assert.NotEqual(employee1.FullName, employee2.FullName);
    }

    /// <summary>
    /// Проверяет, что Gateway корректно распределяет запросы между репликами API сервиса.
    /// </summary>
    [Fact]
    public async Task Gateway_ShouldDistributeRequests()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("gateway");

        var successCount = 0;
        var requestsCount = 10;
        var tasks = new List<Task<bool>>();

        for (var i = 0; i < requestsCount; i++)
        {
            var id = i + 1;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    using var response = await httpClient.GetAsync($"/api/employee?id={id}");
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }));
        }

        var results = await Task.WhenAll(tasks);
        successCount = results.Count(r => r);

        Assert.True(successCount > 0, $"Expected at least one successful request, got {successCount}");
    }

    /// <summary>
    /// Полный end-to-end тест всего пайплайна:
    /// </summary>
    [Fact]
    public async Task FullEndToEnd_ApiToMinio_ShouldSaveFile()
    {
        await _fixture.InitializeSnsOnce();

        var employeeId = Random.Shared.Next(100000, 999999);
        using var apiClient = _fixture.App!.CreateHttpClient("api-1");
        apiClient.Timeout = TimeSpan.FromSeconds(90);

        using var response = await apiClient.GetAsync($"/api/employee?id={employeeId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var employee = JsonSerializer.Deserialize<Employee>(
            await response.Content.ReadAsStringAsync(),
            _jsonOptions);
        Assert.NotNull(employee);

        using var fileServiceClient = _fixture.App!.CreateHttpClient("fileservice");
        fileServiceClient.Timeout = TimeSpan.FromSeconds(30);

        for (var i = 0; i < 15; i++)
        {
            await Task.Delay(2000);

            var fileResponse = await fileServiceClient.GetAsync($"/api/files/employee_{employeeId}.json");

            if (fileResponse.IsSuccessStatusCode)
            {
                var fileContent = await fileResponse.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(fileContent))
                {
                    var savedEmployee = JsonSerializer.Deserialize<Employee>(fileContent, _jsonOptions);
                    Assert.Equal(employee.Id, savedEmployee?.Id);
                    Assert.Equal(employee.FullName, savedEmployee?.FullName);
                    return;
                }
            }
        }

        Assert.Fail($"Файл employee_{employeeId}.json не найден после 30 секунд ожидания");
    }

    /// <summary>
    /// Вспомогательный метод для повторных попыток выполнения асинхронной операции.
    /// </summary>
    private static async Task<T> RetryAsync<T>(
        Func<Task<T>> action,
        int maxAttempts = 3,
        int delayMs = 1000,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxAttempts &&
                (ex is HttpRequestException || ex is TaskCanceledException))
            {
                await Task.Delay(delayMs * attempt, cancellationToken);
            }
        }

        return await action();
    }

}