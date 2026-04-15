using CompanyEmployee.FileService.Services;
using CompanyEmployee.ServiceDefaults;
using Minio;
using Minio.DataModel.Args;

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

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

using (var scope = app.Services.CreateScope())
{
    var minioClient = scope.ServiceProvider.GetRequiredService<IMinioClient>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var bucket = bucketName;

    var bucketExists = await minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucket));
    if (!bucketExists)
    {
        await minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(bucket));
        logger.LogInformation("Bucket {BucketName} created successfully", bucket);
    }
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();