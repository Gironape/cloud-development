using Amazon.SimpleNotificationService;
using Amazon.SQS;
using CompanyEmployee.Api.Services;
using CompanyEmployee.ServiceDefaults;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddRedisDistributedCache("redis");

builder.Services.AddSingleton<IEmployeeGenerator, EmployeeGenerator>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

var awsServiceUrl = builder.Configuration["AWS:ServiceURL"] ?? "http://localhost:4566";
var awsRegion = builder.Configuration["AWS:Region"] ?? "us-east-1";
var awsAccessKey = builder.Configuration["AWS:AccessKeyId"] ?? "test";
var awsSecretKey = builder.Configuration["AWS:SecretAccessKey"] ?? "test";

builder.Services.AddMassTransit(x =>
{
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(awsRegion, h =>
        {
            h.Config(new AmazonSQSConfig { ServiceURL = awsServiceUrl });
            h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = awsServiceUrl });
            h.AccessKey(awsAccessKey);
            h.SecretKey(awsSecretKey);
        });
    });
});

builder.Services.AddSingleton<IPublishEndpoint>(sp => sp.GetRequiredService<IBusControl>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();