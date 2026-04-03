using Amazon.S3;
using CompanyEmployee.FileService.Consumers;
using CompanyEmployee.FileService.Services;
using CompanyEmployee.ServiceDefaults;
using MassTransit;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var awsServiceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";
var awsAccessKey = builder.Configuration["AWS:AccessKeyId"] ?? "test";
var awsSecretKey = builder.Configuration["AWS:SecretAccessKey"] ?? "test";
var bucketName = builder.Configuration["S3:BucketName"] ?? "employee-data";

builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
    awsAccessKey, awsSecretKey,
    new AmazonS3Config
    {
        ServiceURL = awsServiceUrl,
        ForcePathStyle = true
    }));

builder.Services.AddSingleton<IS3FileStorage, S3FileStorage>();
builder.Services.AddSingleton(bucketName);

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

app.Run();