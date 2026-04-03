using CompanyEmployee.FileService.Consumers;
using CompanyEmployee.FileService.Services;
using CompanyEmployee.ServiceDefaults;
using MassTransit;
using Minio;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000";
var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin";
var bucketName = builder.Configuration["MinIO:BucketName"] ?? "employee-data";

builder.Services.AddSingleton(new MinioClient()
    .WithEndpoint(minioEndpoint)
    .WithCredentials(minioAccessKey, minioSecretKey)
    .WithSSL(false)
    .Build());

// Регистрируем IStorageService с реализацией MinioStorageService
builder.Services.AddSingleton<IStorageService, MinioStorageService>();
builder.Services.AddSingleton(bucketName);

var awsServiceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";
var awsAccessKey = builder.Configuration["AWS:AccessKeyId"] ?? "test";
var awsSecretKey = builder.Configuration["AWS:SecretAccessKey"] ?? "test";

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmployeeGeneratedConsumer>();

    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(awsRegion, h =>
        {
            h.Config(new AmazonSQSConfig { ServiceURL = awsServiceUrl });
            h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = awsServiceUrl });
            h.AccessKey(awsAccessKey);
            h.SecretKey(awsSecretKey);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/health", () => "Healthy");

app.MapGet("/api/files/exists/{fileName}", async (string fileName, IStorageService storage) =>
{
    var exists = await storage.FileExistsAsync(bucketName, fileName);
    return exists ? Results.Ok() : Results.NotFound();
});

app.Run();