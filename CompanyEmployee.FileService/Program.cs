using CompanyEmployee.Domain.Entity;
using CompanyEmployee.FileService.Services;
using CompanyEmployee.ServiceDefaults;
using Minio;
using Minio.DataModel.Args;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.AddMinioClient("minio");

var bucketName = builder.Configuration["MinIO:BucketName"] ?? "employee-data";

builder.Services.AddSingleton<IStorageService, MinioStorageService>();
builder.Services.AddSingleton(bucketName);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var minioClient = scope.ServiceProvider.GetRequiredService<IMinioClient>();
    var bucket = bucketName;

    var bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
    if (!bucketExists)
    {
        await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
        Console.WriteLine($"Бакет {bucket} создан");
    }
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapPost("/api/sns/notification", async (HttpRequest request, IStorageService storage, ILogger<Program> logger) =>
{
    using var reader = new StreamReader(request.Body);
    var body = await reader.ReadToEndAsync();

    using var doc = JsonDocument.Parse(body);
    var type = doc.RootElement.GetProperty("Type").GetString();

    if (type == "SubscriptionConfirmation")
    {
        var subscribeUrl = doc.RootElement.GetProperty("SubscribeURL").GetString();
        logger.LogInformation("Подтверждение подписки: {Url}", subscribeUrl);

        using var client = new HttpClient();
        await client.GetAsync(subscribeUrl);

        return Results.Ok(new { message = "Subscription confirmed" });
    }

    if (type == "Notification")
    {
        var messageJson = doc.RootElement.GetProperty("Message").GetString();
        var employee = JsonSerializer.Deserialize<Employee>(messageJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (employee != null)
        {
            var fileName = $"employee_{employee.Id}.json";
            var content = JsonSerializer.SerializeToUtf8Bytes(employee);
            await storage.SaveFileAsync(bucketName, fileName, content);
            logger.LogInformation("Сотрудник {Id} сохранён в MinIO", employee.Id);
        }
    }

    return Results.Ok();
});

app.MapGet("/api/files", async (IStorageService storage) =>
{
    var files = await storage.ListFilesAsync(bucketName);
    return Results.Ok(new { count = files.Count(), files });
});

app.MapGet("/api/files/{fileName}", async (string fileName, IStorageService storage) =>
{
    var content = await storage.GetFileAsync(bucketName, fileName);
    if (content == null) return Results.NotFound();
    var json = Encoding.UTF8.GetString(content);
    return Results.Text(json, "application/json");
});

app.MapGet("/api/files/exists/{fileName}", async (string fileName, IStorageService storage) =>
{
    var exists = await storage.FileExistsAsync(bucketName, fileName);
    return exists ? Results.Ok() : Results.NotFound();
});

app.MapGet("/api/files/{fileName}/metadata", async (string fileName, IStorageService storage) =>
{
    var metadata = await storage.GetFileMetadataAsync(bucketName, fileName);
    return metadata != null ? Results.Ok(metadata) : Results.NotFound();
});

app.Run();