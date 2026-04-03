using CompanyEmployee.FileService.Services;
using CompanyEmployee.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<SnsSettings>(builder.Configuration.GetSection("SNS"));
builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));

builder.Services.AddSingleton<IStorageService, MinioStorageService>();
builder.Services.AddHostedService<SnsConsumerService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapGet("/health", () => "Healthy");

app.Run();