using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CompanyEmployee.Domain.Entity;
using System.Text.Json;

namespace CompanyEmployee.Api.Services;

/// <summary>
/// Сервис для публикации сообщений в SNS.
/// </summary>
/// <param name="snsClient">SNS клиент.</param>
/// <param name="configuration">Конфигурация.</param>
/// <param name="logger">Логгер.</param>
public class SnsPublisherService(
    IAmazonSimpleNotificationService snsClient,
    IConfiguration configuration,
    ILogger<SnsPublisherService> logger)
{
    private readonly string? _topicArn = configuration["SNS:TopicArn"] ?? "arn:aws:sns:us-east-1:000000000000:employee-events";

    public async Task PublishEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_topicArn))
        {
            logger.LogWarning("SNS TopicArn не настроен, публикация пропущена");
            return;
        }

        try
        {
            var message = JsonSerializer.Serialize(employee);
            var publishRequest = new PublishRequest
            {
                TopicArn = _topicArn,
                Message = message,
                Subject = $"Employee-{employee.Id}"
            };
            var response = await snsClient.PublishAsync(publishRequest, cancellationToken);
            logger.LogInformation("Сотрудник {Id} опубликован в SNS, MessageId: {MessageId}", employee.Id, response.MessageId);
        }
        catch (NotFoundException)
        {
            logger.LogWarning("Топик SNS не существует, попытка создать");
            try
            {
                var createTopicRequest = new CreateTopicRequest { Name = "employee-events" };
                var createResponse = await snsClient.CreateTopicAsync(createTopicRequest, cancellationToken);
                var createdTopicArn = createResponse.TopicArn;
                logger.LogInformation("Топик SNS создан: {TopicArn}", createdTopicArn);

                var message = JsonSerializer.Serialize(employee);
                var publishRequest = new PublishRequest
                {
                    TopicArn = createdTopicArn,
                    Message = message,
                    Subject = $"Employee-{employee.Id}"
                };
                var response = await snsClient.PublishAsync(publishRequest, cancellationToken);
                logger.LogInformation("Сотрудник {Id} опубликован в SNS после создания топика, MessageId: {MessageId}", employee.Id, response.MessageId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось создать топик SNS и опубликовать сообщение");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при публикации в SNS");
        }
    }
}