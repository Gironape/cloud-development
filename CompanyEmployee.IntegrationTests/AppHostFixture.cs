using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CompanyEmployee.IntegrationTests;

/// <summary>
/// Фикстура для управления жизненным циклом распределенного приложения Aspire в интеграционных тестах.
/// </summary>
public class AppHostFixture(ILogger<AppHostFixture> logger) : IAsyncLifetime
{
    private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(180);
    private static bool _snsInitialized;
    private static readonly object _snsLock = new();

    /// <summary>
    /// Экземпляр запущенного распределенного приложения Aspire.
    /// </summary>
    public DistributedApplication? App { get; private set; }

    /// <summary>
    /// Инициализирует распределенное приложение перед запуском тестов.
    /// </summary>
    public async Task InitializeAsync()
    {
        logger.LogInformation("Initializing AppHost for integration tests");

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CompanyEmployee_AppHost>();

        appHost.Configuration["DcpPublisher:RandomizePorts"] = "false";
        appHost.Configuration["ASPIRE_ENVIRONMENT"] = "Testing";

        appHost.Services.ConfigureHttpClientDefaults(http =>
            http.AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
                options.Retry.MaxRetryAttempts = 10;
                options.Retry.Delay = TimeSpan.FromSeconds(3);
            }));

        appHost.Services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        App = await appHost.BuildAsync();
        await App.StartAsync();

        logger.LogInformation("Waiting for resources to become healthy");

        await App.ResourceNotifications.WaitForResourceHealthyAsync("redis").WaitAsync(_defaultTimeout);
        logger.LogInformation("Redis is healthy");

        await App.ResourceNotifications.WaitForResourceHealthyAsync("minio").WaitAsync(_defaultTimeout);
        logger.LogInformation("MinIO is healthy");

        await App.ResourceNotifications.WaitForResourceHealthyAsync("localstack").WaitAsync(_defaultTimeout);
        logger.LogInformation("LocalStack is healthy");

        await App.ResourceNotifications.WaitForResourceHealthyAsync("fileservice").WaitAsync(_defaultTimeout);
        logger.LogInformation("FileService is healthy");

        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway").WaitAsync(_defaultTimeout);
        logger.LogInformation("Gateway is healthy");

        for (var i = 1; i <= 5; i++)
        {
            await App.ResourceNotifications.WaitForResourceHealthyAsync($"api-{i}").WaitAsync(_defaultTimeout);
            logger.LogInformation("API replica {ReplicaNumber} is healthy", i);
        }

        logger.LogInformation("All resources are healthy, waiting for stabilization");
        await Task.Delay(5000);

        await InitializeSnsOnce();
    }

    /// <summary>
    /// Однократно инициализирует SNS топик и подписку в LocalStack.
    /// </summary>
    public async Task InitializeSnsOnce()
    {
        lock (_snsLock)
        {
            if (_snsInitialized) return;
        }

        try
        {
            logger.LogInformation("Starting SNS initialization");

            var localstackEndpoint = App!.GetEndpoint("localstack", "localstack");
            var fileServiceEndpoint = App!.GetEndpoint("fileservice", "http");

            var localStackPort = localstackEndpoint.Port.ToString();
            var fileServicePort = fileServiceEndpoint.Port.ToString();

            logger.LogInformation("LocalStack port: {LocalStackPort}, FileService HTTP port: {FileServicePort}",
                localStackPort, fileServicePort);

            var fileServiceUrl = $"http://host.docker.internal:{fileServicePort}";
            logger.LogInformation("Subscription endpoint: {Endpoint}", $"{fileServiceUrl}/api/sns/notification");

            logger.LogInformation("Waiting for LocalStack health check");
            using var httpClient = new HttpClient();
            for (var i = 0; i < 30; i++)
            {
                try
                {
                    var health = await httpClient.GetAsync($"{localstackEndpoint.AbsoluteUri}/_localstack/health");
                    if (health.IsSuccessStatusCode) break;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "LocalStack health check failed, attempt {Attempt}", i + 1);
                }
                await Task.Delay(2000);
            }

            logger.LogInformation("Creating SNS client for LocalStack at {ServiceUrl}", localstackEndpoint.AbsoluteUri);
            var snsConfig = new AmazonSimpleNotificationServiceConfig
            {
                ServiceURL = localstackEndpoint.AbsoluteUri,
                UseHttp = true,
                AuthenticationRegion = "us-east-1"
            };

            using var snsClient = new AmazonSimpleNotificationServiceClient("test", "test", snsConfig);

            logger.LogInformation("Creating SNS topic: {TopicName}", "employee-events");
            var createResponse = await snsClient.CreateTopicAsync(new CreateTopicRequest
            {
                Name = "employee-events"
            });

            var topicArn = createResponse.TopicArn;
            logger.LogInformation("SNS topic created: {TopicArn}", topicArn);

            logger.LogInformation("Creating SNS subscription to {Endpoint}", $"{fileServiceUrl}/api/sns/notification");
            await snsClient.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "http",
                Endpoint = $"{fileServiceUrl}/api/sns/notification"
            });

            logger.LogInformation("SNS subscription created successfully");
            logger.LogInformation("Waiting for subscription confirmation");
            await Task.Delay(5000);

            lock (_snsLock)
            {
                _snsInitialized = true;
            }

            logger.LogInformation("SNS initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SNS initialization failed");
            throw;
        }
    }

    /// <summary>
    /// Освобождает ресурсы распределенного приложения после завершения всех тестов.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (App != null)
        {
            try
            {
                logger.LogInformation("Disposing AppHost");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await App.DisposeAsync().AsTask().WaitAsync(cts.Token);
                logger.LogInformation("AppHost disposed successfully");
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("App disposal timed out");
            }
        }
    }
}