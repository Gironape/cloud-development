using System.Text.Json;
using CompanyEmployee.Domain.Entity;
using CompanyEmployee.FileService.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompanyEmployee.FileService.Controllers;

/// <summary>
/// Контроллер для обработки SNS уведомлений от LocalStack.
/// </summary>
[ApiController]
[Route("api/sns")]
public class SnsController(
    IStorageService storage,
    string bucketName,
    IHttpClientFactory httpClientFactory,
    ILogger<SnsController> logger,
    IConfiguration configuration) : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Принимает SNS уведомления от LocalStack и обрабатывает их в зависимости от типа.
    /// </summary>
    [HttpPost("notification")]
    public async Task<IActionResult> ReceiveNotification()
    {
        try
        {
            logger.LogInformation("SNS notification received");

            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();
            logger.LogDebug("SNS body: {Body}", body);

            if (!TryParseJson(body, out var doc))
            {
                return BadRequest("Invalid JSON payload");
            }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("Type", out var typeElement))
                {
                    logger.LogWarning("Missing Type property in SNS payload");
                    return BadRequest("Missing Type property");
                }

                var type = typeElement.GetString();
                logger.LogInformation("SNS notification type: {Type}", type);

                return type?.ToLowerInvariant() switch
                {
                    "subscriptionconfirmation" => await HandleSubscriptionConfirmationAsync(doc),
                    "notification" => await HandleNotificationAsync(doc),
                    _ => HandleUnknownType(type)
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error processing SNS notification");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private bool TryParseJson(string body, out JsonDocument? document)
    {
        try
        {
            document = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to parse SNS payload as JSON");
            document = null;
            return false;
        }
    }

    private async Task<IActionResult> HandleSubscriptionConfirmationAsync(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("SubscribeURL", out var urlElement))
        {
            logger.LogWarning("Missing SubscribeURL in subscription confirmation");
            return BadRequest("Missing SubscribeURL");
        }

        var subscribeUrl = urlElement.GetString();
        if (string.IsNullOrWhiteSpace(subscribeUrl))
        {
            logger.LogWarning("Empty SubscribeURL in subscription confirmation");
            return BadRequest("Empty SubscribeURL");
        }

        logger.LogDebug("Original SubscribeURL: {Url}", subscribeUrl);

        var fixedUrl = FixLocalStackUrl(subscribeUrl);
        logger.LogInformation("Confirming subscription at: {Url}", fixedUrl);

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);

        try
        {
            using var response = await client.GetAsync(fixedUrl);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("SNS subscription confirmed successfully");
                return Ok(new { message = "Subscription confirmed" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed to confirm subscription. Status: {StatusCode}, Response: {Response}",
                response.StatusCode, responseContent);
            return StatusCode(500, new { error = "Subscription confirmation failed" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception during subscription confirmation request to {Url}", fixedUrl);
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task<IActionResult> HandleNotificationAsync(JsonDocument doc)
    {
        if (!doc.RootElement.TryGetProperty("Message", out var messageElement))
        {
            logger.LogWarning("Missing Message property in SNS notification");
            return BadRequest("Missing Message property");
        }

        var messageJson = messageElement.GetString();
        if (string.IsNullOrWhiteSpace(messageJson))
        {
            logger.LogWarning("Empty Message in SNS notification");
            return Ok();
        }

        logger.LogDebug("SNS message content: {Message}", messageJson);

        var employee = JsonSerializer.Deserialize<Employee>(messageJson, _jsonOptions);
        if (employee is null)
        {
            logger.LogWarning("Failed to deserialize employee from SNS message");
            return Ok();
        }

        var fileName = $"employee_{employee.Id}.json";
        var content = JsonSerializer.SerializeToUtf8Bytes(employee);

        await storage.SaveFileAsync(bucketName, fileName, content);
        logger.LogInformation("Employee {EmployeeId} saved to MinIO as {FileName}", employee.Id, fileName);

        return Ok();
    }

    private IActionResult HandleUnknownType(string? type)
    {
        logger.LogInformation("Unknown SNS notification type: {Type}", type);
        return Ok();
    }

    private string FixLocalStackUrl(string originalUrl)
    {
        var localStackPort = configuration["LocalStack:Port"] ?? "4566";
        logger.LogDebug("Using LocalStack port: {Port}", localStackPort);

        return originalUrl
            .Replace("localhost.localstack.cloud", "localhost")
            .Replace(":4566", $":{localStackPort}");
    }
}