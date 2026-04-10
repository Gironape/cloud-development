using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CompanyEmployee.IntegrationTests;

/// <summary>
/// Фикстура для запуска Aspire приложения в тестах.
/// </summary>
public class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(120);
    public DistributedApplication? App { get; private set; }

    public async Task InitializeAsync()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.CompanyEmployee_AppHost>();

        appHost.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(120);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(120);
            }));

        App = await appHost.BuildAsync();
        await App.StartAsync();

        await App.ResourceNotifications.WaitForResourceHealthyAsync("redis").WaitAsync(_defaultTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("minio").WaitAsync(_defaultTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("localstack").WaitAsync(_defaultTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway").WaitAsync(_defaultTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("fileservice").WaitAsync(_defaultTimeout);

        for (var i = 1; i <= 5; i++)
        {
            await App.ResourceNotifications.WaitForResourceHealthyAsync($"api-{i}").WaitAsync(_defaultTimeout);
        }
    }

    public async Task DisposeAsync()
    {
        if (App != null)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await App.DisposeAsync().AsTask().WaitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}