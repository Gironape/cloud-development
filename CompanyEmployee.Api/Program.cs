using Amazon.SimpleNotificationService;
using CompanyEmployee.Api.Services;
using CompanyEmployee.ServiceDefaults;

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

var snsConfig = new AmazonSimpleNotificationServiceConfig
{
    ServiceURL = awsServiceUrl,
    AuthenticationRegion = awsRegion,
    UseHttp = true
};

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    new AmazonSimpleNotificationServiceClient(awsAccessKey, awsSecretKey, snsConfig));

builder.Services.AddSingleton<SnsPublisherService>();

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