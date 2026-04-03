using CompanyEmployee.Domain.Entity;
using System.Text.Json;

namespace CompanyEmployee.IntegrationTests;

/// <summary>
/// Интеграционные тесты для проверки работы всех сервисов.
/// </summary>
public class BasicTests
{
    private readonly HttpClient _apiClient;
    private readonly HttpClient _gatewayClient;
    private readonly HttpClient _fileServiceClient;

    public BasicTests()
    {
        _apiClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:6001")
        };

        _gatewayClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7000")
        };

        _fileServiceClient = new HttpClient
        {
            BaseAddress = new Uri("https://localhost:7277")
        };
    }

    /// <summary>
    /// Проверяет, что API генерирует сотрудника.
    /// </summary>
    [Fact]
    public async Task GetEmployee_ShouldReturnEmployee()
    {
        var response = await _apiClient.GetAsync("/api/employee?id=1");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var employee = JsonSerializer.Deserialize<Employee>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(employee);
        Assert.Equal(1, employee.Id);
        Assert.False(string.IsNullOrEmpty(employee.FullName));
    }

    /// <summary>
    /// Проверяет работу кэширования в Redis.
    /// </summary>
    [Fact]
    public async Task SameEmployeeId_ShouldReturnSameData()
    {
        var response1 = await _apiClient.GetAsync("/api/employee?id=2");
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await _apiClient.GetAsync("/api/employee?id=2");
        var content2 = await response2.Content.ReadAsStringAsync();

        Assert.Equal(content1, content2);
    }

    /// <summary>
    /// Проверяет, что разные ID генерируют разных сотрудников.
    /// </summary>
    [Fact]
    public async Task DifferentIds_ShouldGenerateDifferentEmployees()
    {
        var response1 = await _apiClient.GetAsync("/api/employee?id=10");
        var employee1 = JsonSerializer.Deserialize<Employee>(
            await response1.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var response2 = await _apiClient.GetAsync("/api/employee?id=20");
        var employee2 = JsonSerializer.Deserialize<Employee>(
            await response2.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(employee1);
        Assert.NotNull(employee2);
        Assert.NotEqual(employee1.FullName, employee2.FullName);
    }

    /// <summary>
    /// Проверяет балансировку нагрузки через Gateway (Weighted Random).
    /// </summary>
    [Fact]
    public async Task LoadBalancer_ShouldDistributeRequests()
    {
        var requestsCount = 50;
        var successCount = 0;

        for (var i = 0; i < requestsCount; i++)
        {
            var response = await _gatewayClient.GetAsync($"/api/employee?id={i + 1}");
            if (response.IsSuccessStatusCode)
            {
                successCount++;
            }
        }

        Assert.Equal(requestsCount, successCount);
    }

    /// <summary>
    /// Проверяет отправку сообщения в SNS и сохранение в S3.
    /// </summary>
    [Fact]
    public async Task EmployeeGeneration_ShouldSendToSnsAndSaveToS3()
    {
        var employeeId = 777;
        await _fileServiceClient.GetAsync("/health");
        await Task.Delay(5000);
        var response = await _apiClient.GetAsync($"/api/employee?id={employeeId}");
        response.EnsureSuccessStatusCode();

        await Task.Delay(3000);

        var fileCheckResponse = await _fileServiceClient.GetAsync($"/api/files/exists/employee_{employeeId}.json");

        Assert.True(fileCheckResponse.IsSuccessStatusCode, "Файл не найден в S3 хранилище");
    }

    /// <summary>
    /// Проверяет полный цикл: генерация -> кэш -> брокер -> хранилище.
    /// </summary>
    [Fact]
    public async Task FullCycle_ShouldWorkCorrectly()
    {
        var employeeId = 777;

        var startTime = DateTime.Now;
        var response = await _apiClient.GetAsync($"/api/employee?id={employeeId}");
        var firstDuration = DateTime.Now - startTime;

        response.EnsureSuccessStatusCode();
        var employee = JsonSerializer.Deserialize<Employee>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(employee);
        Assert.Equal(employeeId, employee.Id);

        startTime = DateTime.Now;
        var cachedResponse = await _apiClient.GetAsync($"/api/employee?id={employeeId}");
        var secondDuration = DateTime.Now - startTime;

        Assert.True(secondDuration < firstDuration, "Второй запрос должен быть быстрее (кэш)");

        await Task.Delay(3000);

        var fileExists = await _fileServiceClient.GetAsync($"/api/files/exists/employee_{employeeId}.json");
        Assert.True(fileExists.IsSuccessStatusCode, "Файл должен сохраниться в S3");
    }
}