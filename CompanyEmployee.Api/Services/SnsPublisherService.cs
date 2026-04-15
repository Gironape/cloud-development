using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CompanyEmployee.Domain.Entity;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для публикации сообщений о сотрудниках в SNS.
/// </summary>
public class SnsPublisherService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsPublisherService> logger)
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _topicArn = configuration["SNS:TopicArn"]
        ?? "arn:aws:sns:us-east-1:000000000000:employee-events";

    /// <summary>
    /// Публикует данные сотрудника в SNS топик.
    /// </summary>
    public async Task PublishEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Publishing employee {EmployeeId} to SNS topic {TopicArn}", employee.Id, _topicArn);

            var message = JsonSerializer.Serialize(employee, _jsonOptions);
            var publishRequest = new PublishRequest
            {
                TopicArn = _topicArn,
                Message = message,
                Subject = $"Employee-{employee.Id}"
            };

            var response = await snsClient.PublishAsync(publishRequest, cancellationToken);
            logger.LogInformation("Employee {EmployeeId} published to SNS, MessageId: {MessageId}",
                employee.Id, response.MessageId);
        }
        catch (NotFoundException)
        {
            logger.LogWarning("SNS topic not found, attempting to create topic");
            await TryCreateTopicAndPublishAsync(employee, cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "SNS request timed out for employee {EmployeeId}", employee.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish employee {EmployeeId} to SNS", employee.Id);
        }
    }

    private async Task TryCreateTopicAndPublishAsync(Employee employee, CancellationToken cancellationToken)
    {
        try
        {
            var createTopicRequest = new CreateTopicRequest { Name = "employee-events" };
            var createResponse = await snsClient.CreateTopicAsync(createTopicRequest, cancellationToken);
            var createdTopicArn = createResponse.TopicArn;

            logger.LogInformation("SNS topic created: {TopicArn}", createdTopicArn);

            var message = JsonSerializer.Serialize(employee, _jsonOptions);
            var publishRequest = new PublishRequest
            {
                TopicArn = createdTopicArn,
                Message = message,
                Subject = $"Employee-{employee.Id}"
            };

            var response = await snsClient.PublishAsync(publishRequest, cancellationToken);
            logger.LogInformation("Employee {EmployeeId} published after topic creation, MessageId: {MessageId}",
                employee.Id, response.MessageId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create SNS topic and publish employee {EmployeeId}", employee.Id);
        }
    }
}