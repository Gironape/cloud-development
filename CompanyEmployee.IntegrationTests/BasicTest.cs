using Aspire.Hosting.Testing;
using CompanyEmployee.Domain.Entity;
using System.Net;
using System.Text.Json;

namespace CompanyEmployee.IntegrationTests;

public class BasicTest : IClassFixture<AppHostFixture>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly AppHostFixture _fixture;

    public BasicTest(AppHostFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Api_HealthCheck_ReturnsHealthy()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("api-1");
        using var response = await httpClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task FileService_HealthCheck_ReturnsHealthy()
    {
        using var httpClient = _fixture.App!.CreateHttpClient("fileservice");
        using var response = await httpClient.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

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
}